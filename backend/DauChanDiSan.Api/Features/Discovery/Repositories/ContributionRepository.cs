using System.Data;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Extensions;
using DauChanDiSan.Api.Features.Discovery.DTOs;
using DauChanDiSan.Api.Helpers;
using DauChanDiSan.Api.Shared;
using DauChanDiSan.Api.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Discovery.Repositories;

public sealed class ContributionRepository(DauChanDiSanDbContext db)
{
    public Task<PageResult<ContributionDto>> ListAsync(int? ownerId, string? status, int page, CancellationToken ct)
    {
        var rows = db.DongGopDiSans.AsNoTracking();
        if (ownerId is not null) rows = rows.Where(row => row.NguoiDungId == ownerId);
        if (status is not null) rows = rows.Where(row => row.TrangThai == status);
        return rows.OrderByDescending(row => row.DongGopId).ToPageAsync(page, 20, ContributionDto.Projection, ct);
    }

    public async Task<ContributionDto> FindAsync(long id, int? ownerId, CancellationToken ct) =>
        await db.DongGopDiSans.AsNoTracking()
            .Where(row => row.DongGopId == id && (ownerId == null || row.NguoiDungId == ownerId))
            .Select(ContributionDto.Projection).SingleOrDefaultAsync(ct)
        ?? throw ApiFault.Missing("Không tìm thấy đóng góp.");

    public async Task<(ContributionDto Row, bool Created)> SubmitAsync(int ownerId, ContributionInput input, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var row = new DongGopDiSan
        {
            NguoiDungId = ownerId, MaYeuCau = input.MaYeuCau, Loai = input.Loai, DiSanId = input.DiSanId,
            TinhThanhId = input.TinhThanhId, LoaiDiSanId = input.LoaiDiSanId,
            TieuDe = input.TieuDe.Trim(), NoiDung = input.NoiDung.Trim(), DiaChi = input.DiaChi?.Trim(),
            ViDo = RoundCoordinate(input.ViDo), KinhDo = RoundCoordinate(input.KinhDo), AnhPath = input.AnhPath,
            UrlNguon = input.UrlNguon?.Trim(), TrangThai = "CHO_DUYET", TaoLucUtc = now, CapNhatLucUtc = now
        };
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var existing = await db.DongGopDiSans.FromSqlInterpolated(
            $"SELECT * FROM dbo.DongGopDiSan WITH (UPDLOCK,HOLDLOCK) WHERE NguoiDungId={ownerId} AND MaYeuCau={input.MaYeuCau}")
            .AsNoTracking().SingleOrDefaultAsync(ct);
        if (existing is not null)
        {
            if ((row.Loai, row.DiSanId, row.TinhThanhId, row.LoaiDiSanId, row.TieuDe, row.NoiDung, row.DiaChi,
                    row.ViDo, row.KinhDo, row.AnhPath, row.UrlNguon)
                != (existing.Loai, existing.DiSanId, existing.TinhThanhId, existing.LoaiDiSanId,
                    existing.TieuDe, existing.NoiDung, existing.DiaChi, existing.ViDo, existing.KinhDo, existing.AnhPath, existing.UrlNguon))
                throw new ApiFault(409, "Mã yêu cầu đã dùng cho nội dung đóng góp khác.");
            await transaction.CommitAsync(ct);
            return (await FindAsync(existing.DongGopId, ownerId, ct), false);
        }

        if (row.DiSanId is int heritageId)
            await EnsurePublicHeritageAsync(heritageId, ct);
        else
            await ValidateNewHeritageAsync(row, ct);

        db.DongGopDiSans.Add(row);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (await FindAsync(row.DongGopId, ownerId, ct), true);
    }

    public async Task WithdrawAsync(int ownerId, long id, CancellationToken ct)
    {
        var row = await db.DongGopDiSans.SingleOrDefaultAsync(x => x.DongGopId == id && x.NguoiDungId == ownerId, ct)
            ?? throw ApiFault.Missing("Không tìm thấy đóng góp.");
        if (row.TrangThai == "DA_RUT") return;
        EnsurePending(row);
        row.TrangThai = "DA_RUT";
        row.CapNhatLucUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ContributionDto> ReviewAsync(int adminId, long id, ReviewContributionInput input, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var row = await db.DongGopDiSans.FromSqlInterpolated(
                $"SELECT * FROM dbo.DongGopDiSan WITH (UPDLOCK,HOLDLOCK) WHERE DongGopId={id}")
            .SingleOrDefaultAsync(ct) ?? throw ApiFault.Missing("Không tìm thấy đóng góp.");
        EnsurePending(row);
        if (row.NguoiDungId == adminId)
            throw ApiFault.Denied();

        var now = DateTime.UtcNow;
        if (input.QuyetDinh == "DA_DUYET")
            await PublishAsync(row, adminId, input.Slug, now, ct);
        else
            row.LyDo = input.LyDo!.Trim();

        row.TrangThai = input.QuyetDinh;
        row.NguoiDuyetId = adminId;
        row.DuyetLucUtc = now;
        row.CapNhatLucUtc = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await FindAsync(id, null, ct);
    }

    private async Task PublishAsync(DongGopDiSan row, int adminId, string? slug, DateTime now, CancellationToken ct)
    {
        int heritageId;
        if (row.DiSanId is int existingId)
        {
            if (slug is not null) throw ApiFault.Invalid("Bổ sung dùng trang di sản đang có, hãy bỏ trống slug.");
            await EnsurePublicHeritageAsync(existingId, ct);
            heritageId = existingId;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(slug)) throw ApiFault.Invalid("Cần slug khi duyệt di sản mới.");
            await ValidateNewHeritageAsync(row, ct);
            var heritage = new DiSan
            {
                TinhThanhId = row.TinhThanhId!.Value, LoaiDiSanId = row.LoaiDiSanId!.Value,
                Ten = row.TieuDe, Slug = slug, TomTat = TextHelper.Limit(row.NoiDung, 1000),
                NoiDung = row.NoiDung, DiaChi = row.DiaChi!, ViDo = row.ViDo!.Value, KinhDo = row.KinhDo!.Value,
                AnhBiaPath = row.AnhPath, TrangThai = "CONG_KHAI", BanKinhCheckInM = 150, SaiSoGpsToiDaM = 50
            };
            db.DiSans.Add(heritage);
            await db.SaveChangesAsync(ct);
            heritageId = heritage.DiSanId;
        }

        if (row.DiSanId is not null || row.UrlNguon is not null)
        {
            var source = new TuLieuDiSan
            {
                DiSanId = heritageId, TieuDe = row.DiSanId is null ? "Nguồn tham khảo từ cộng đồng" : row.TieuDe,
                TrichYeu = row.DiSanId is null ? "Nguồn được gửi kèm khi đề xuất di sản." : row.NoiDung,
                Nguon = "Đóng góp cộng đồng", UrlNguon = row.UrlNguon,
                NguoiDuyetId = adminId, DuyetLucUtc = now, CongKhai = true
            };
            db.TuLieuDiSans.Add(source);
            if (row.DiSanId is not null && row.AnhPath is not null)
            {
                var last = await db.MediaDiSans.Where(x => x.DiSanId == heritageId)
                    .MaxAsync(x => (short?)x.ThuTu, ct) ?? 0;
                if (last == short.MaxValue) throw new ApiFault(409, "Album của di sản đã đầy.");
                db.MediaDiSans.Add(new MediaDiSan
                {
                    DiSanId = heritageId, AnhPath = row.AnhPath, ChuThich = row.TieuDe, ThuTu = (short)(last + 1)
                });
            }
            await db.SaveChangesAsync(ct);
            row.TuLieuId = source.TuLieuId;
        }
        row.DiSanCongKhaiId = heritageId;
    }

    private async Task ValidateNewHeritageAsync(DongGopDiSan row, CancellationToken ct)
    {
        if (!await db.TinhThanhs.AnyAsync(x => x.TinhThanhId == row.TinhThanhId && x.HoatDong, ct)
            || !await db.LoaiDiSans.AnyAsync(x => x.LoaiDiSanId == row.LoaiDiSanId, ct))
            throw ApiFault.Invalid("Tỉnh thành hoặc loại di sản không khả dụng.");
        if (await db.DiSans.AnyAsync(x => x.TinhThanhId == row.TinhThanhId
            && EF.Functions.Collate(x.Ten, "Latin1_General_100_CI_AI_SC") == row.TieuDe, ct))
            throw new ApiFault(409, "Đã có di sản trùng tên tại tỉnh thành này. Hãy gửi bài bổ sung.");
    }

    private async Task EnsurePublicHeritageAsync(int id, CancellationToken ct)
    {
        if (!await db.VwDiSanCongKhais.AnyAsync(x => x.DiSanId == id, ct))
            throw ApiFault.Missing("Di sản được chọn chưa công khai hoặc không tồn tại.");
    }

    private static void EnsurePending(DongGopDiSan row)
    {
        if (row.TrangThai != "CHO_DUYET")
            throw new ApiFault(409, "Đóng góp không còn chờ duyệt.");
    }

    private static decimal? RoundCoordinate(decimal? value) =>
        value is decimal n ? Math.Round(n, 6, MidpointRounding.AwayFromZero) : null;
}
