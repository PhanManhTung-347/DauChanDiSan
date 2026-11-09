using System.Linq.Expressions;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Extensions;
using DauChanDiSan.Api.Features.CheckIns.DTOs;
using DauChanDiSan.Api.Shared;
using DauChanDiSan.Api.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.CheckIns.Repositories;

public sealed class CheckInRepository(DauChanDiSanDbContext db, SqlProcedures sql)
{
    public Task<PageResult<CheckInDto>> ListAsync(int ownerId, int page, CancellationToken ct) =>
        Owned(ownerId).OrderByDescending(x => x.CheckInId).ToPageAsync(page, 30, Projection, ct);

    public async Task<CheckInDto> DetailAsync(int ownerId, long id, CancellationToken ct) =>
        await Owned(ownerId).Where(x => x.CheckInId == id).Select(Projection).SingleOrDefaultAsync(ct)
        ?? throw ApiFault.Missing("Không tìm thấy check-in.");

    public async Task<CheckInReceipt> RecordAsync(int ownerId, CheckInInput input, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var result = (await sql.RunAsync("dbo.usp_CheckInDiSan", ct, ("NguoiDungId", ownerId),
            ("DiSanId", input.DiSanId), ("MaYeuCau", input.MaYeuCau), ("ViDo", input.ViDo), ("KinhDo", input.KinhDo),
            ("SaiSoGpsM", input.SaiSoGpsM), ("GhiChu", input.GhiChu), ("AnhPath", input.AnhPath), ("VideoPath", input.VideoPath))).Single();
        var now = DateTime.UtcNow;
        var missions = await db.NhiemVus.FromSqlInterpolated($"""
                SELECT * FROM dbo.NhiemVu WITH (HOLDLOCK)
                WHERE CongKhai=1 AND Loai='DEM_DI_SAN' AND BatDauUtc<={now}
                  AND (KetThucUtc IS NULL OR KetThucUtc>{now})
                """)
            .OrderBy(x => x.NhiemVuId).Select(x => x.NhiemVuId).ToListAsync(ct);
        foreach (var mission in missions)
            await sql.RunAsync("dbo.usp_CapNhatTienDoNhiemVu", ct, ("NguoiDungId", ownerId),
                ("NhiemVuId", mission), ("TraKetQua", false));
        await tx.CommitAsync(ct);
        return new CheckInReceipt((long)result["CheckInId"]!, (int)result["DiSanId"]!,
            (DateTime)result["ThoiGianUtc"]!, (DateOnly?)result["NgayDiaPhuong"],
            (decimal)result["KhoangCachM"]!, (short)result["BanKinhApDungM"]!);
    }

    public Task AttachAsync(int ownerId, long id, long tripId, CancellationToken ct) =>
        sql.RunAsync("dbo.usp_GanCheckInVaoHanhTrinh", ct, ("NguoiDungId", ownerId),
            ("HanhTrinhId", tripId), ("CheckInId", id));

    private IQueryable<CheckInDiSan> Owned(int ownerId) =>
        db.CheckInDiSans.AsNoTracking().Where(x => x.NguoiDungId == ownerId);

    private static readonly Expression<Func<CheckInDiSan, CheckInDto>> Projection =
        x => new CheckInDto(x.CheckInId, x.DiSanId, x.DiSan.Ten, x.NgayDiaPhuong, x.ThoiGianUtc,
            x.KhoangCachM, x.GhiChu, x.AnhPath, x.VideoPath);
}
