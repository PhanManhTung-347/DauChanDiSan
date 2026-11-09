using DauChanDiSan.Api.Extensions;
using DauChanDiSan.Api.Validation;
using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Discovery;


[ApiController, Route("api/di-san")]
public sealed class DiscoveryController(DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery, StringLength(100)] string? q, int? tinhThanhId, int? loaiDiSanId,
        [FromQuery, Range(1, 5000)] int trang = 1, [FromQuery, Range(1, 100)] int kichThuoc = 20, CancellationToken ct = default)
    {
        var query = db.VwDiSanCongKhais.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => EF.Functions.Collate(x.Ten, "Latin1_General_100_CI_AI_SC").Contains(q)
                || EF.Functions.Collate(x.TomTat, "Latin1_General_100_CI_AI_SC").Contains(q));
        if (tinhThanhId is not null) query = query.Where(x => x.TinhThanhId == tinhThanhId);
        if (loaiDiSanId is not null) query = query.Where(x => x.LoaiDiSanId == loaiDiSanId);
        return Ok(await query.OrderBy(x => x.Ten).ThenBy(x => x.DiSanId).ToPageAsync(trang, kichThuoc, ct));
    }

    [HttpGet("gan")]
    public async Task<IActionResult> Nearby([FromQuery, Required, Range(-90, 90)] decimal? viDo,
        [FromQuery, Required, Range(-180, 180)] decimal? kinhDo, [FromQuery, Range(1, 200000)] int banKinhM = 5000,
        CancellationToken ct = default) =>
        Ok(await sql.QueryAsync("""
            SELECT TOP (100) DiSanId, Ten, Slug, TinhThanh, ViDo, KinhDo, AnhBiaPath, KhoangCachM
            FROM dbo.fn_DiSanGan(@ViDo,@KinhDo,@BanKinhM) ORDER BY KhoangCachM, DiSanId;
            """, ct, ("ViDo", viDo), ("KinhDo", kinhDo), ("BanKinhM", banKinhM)));

    [HttpGet("danh-muc")]
    public async Task<IActionResult> Catalogs(CancellationToken ct) => Ok(new
    {
        tinhThanh = await db.TinhThanhs.AsNoTracking().Where(x => x.HoatDong).OrderBy(x => x.Ten).ToListAsync(ct),
        loaiDiSan = await db.LoaiDiSans.AsNoTracking().OrderBy(x => x.Ten).ToListAsync(ct),
        chuDe = await db.ChuDes.AsNoTracking().OrderBy(x => x.Ten).ToListAsync(ct)
    });

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct)
    {
        var row = await db.VwDiSanCongKhais.SingleOrDefaultAsync(x => x.Slug == slug, ct)
            ?? throw ApiFault.Missing("Di sản không tồn tại.");
        var id = row.DiSanId;
        return Ok(new
        {
            diSan = row,
            gioThamQuan = await db.GioThamQuans.AsNoTracking().Where(x => x.DiSanId == id)
                .OrderBy(x => x.ThuTrongTuan).ThenBy(x => x.GioMo).ToListAsync(ct),
            media = await db.MediaDiSans.AsNoTracking().Where(x => x.DiSanId == id).OrderBy(x => x.ThuTu).ToListAsync(ct),
            tuLieu = await db.TuLieuDiSans.Where(x => x.DiSanId == id && x.CongKhai).OrderBy(x => x.TuLieuId)
                .Select(x => new { x.TuLieuId, x.TieuDe, x.TrichYeu, x.Nguon, x.UrlNguon }).ToListAsync(ct)
        });
    }

    [HttpGet("yeu-thich/toi"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> Favorites(CancellationToken ct) =>
        Ok(await (from f in db.DiSanYeuThiches.AsNoTracking()
            join d in db.VwDiSanCongKhais on f.DiSanId equals d.DiSanId
            where f.NguoiDungId == user.Id orderby f.LuuLucUtc descending select d).ToListAsync(ct));

    [HttpPut("{id:int}/yeu-thich"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> AddFavorite(int id, CancellationToken ct)
    {
        await EnsurePublic(id, ct);
        await sql.QueryAsync("""
            INSERT dbo.DiSanYeuThich(NguoiDungId, DiSanId)
            SELECT @UserId,@Id WHERE NOT EXISTS (
                SELECT 1 FROM dbo.DiSanYeuThich WITH (UPDLOCK,HOLDLOCK)
                WHERE NguoiDungId=@UserId AND DiSanId=@Id);
            """, ct, ("UserId", user.Id), ("Id", id));
        return NoContent();
    }

    [HttpDelete("{id:int}/yeu-thich"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> RemoveFavorite(int id, CancellationToken ct)
    {
        await db.DiSanYeuThiches.Where(x => x.NguoiDungId == user.Id && x.DiSanId == id).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:int}/danh-gia")]
    public async Task<IActionResult> Reviews(int id, [FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default)
    {
        await EnsurePublic(id, ct);
        return Ok(await db.DanhGiaDiSans.AsNoTracking().Where(x => x.DiSanId == id && x.CongKhai)
            .OrderByDescending(x => x.TaoLucUtc).ThenBy(x => x.NguoiDungId).Skip((trang - 1) * 20).Take(20)
            .Select(x => new { x.SoSao, x.NoiDung, x.AnhPath, x.VideoPath, x.TaoLucUtc, tenNguoiViet = x.CheckInDiSan.NguoiDung.HoTen })
            .ToListAsync(ct));
    }

    [HttpGet("{id:int}/danh-gia/toi"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> MyReview(int id, CancellationToken ct)
    {
        var canReview = await db.CheckInDiSans.AnyAsync(x => x.DiSanId == id && x.NguoiDungId == user.Id, ct);
        var review = await db.DanhGiaDiSans.AsNoTracking().Where(x => x.DiSanId == id && x.NguoiDungId == user.Id)
            .Select(x => new { x.SoSao, x.NoiDung, x.CongKhai }).SingleOrDefaultAsync(ct);
        return Ok(new { daCheckIn = canReview, danhGia = review });
    }

    [HttpPut("{id:int}/danh-gia"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> Review(int id, ReviewInput input, CancellationToken ct)
    {
        await EnsurePublic(id, ct);
        var checkIn = await db.CheckInDiSans.Where(x => x.DiSanId == id && x.NguoiDungId == user.Id)
            .OrderByDescending(x => x.CheckInId).Select(x => (long?)x.CheckInId).FirstOrDefaultAsync(ct)
            ?? throw ApiFault.Invalid("Bạn cần check-in trước khi viết đánh giá.");
        var row = await db.DanhGiaDiSans.FindAsync([user.Id, id], ct);
        if (row?.CongKhai == false)
            throw new ApiFault(409, "Đánh giá đang được ẩn bởi bộ phận kiểm duyệt.");
        if (row is null)
        {
            row = new DanhGiaDiSan { NguoiDungId = user.Id, DiSanId = id, CongKhai = true };
            db.DanhGiaDiSans.Add(row);
        }
        if (row.AnhPath != input.AnhPath) MediaValidation.RequireOwner(user.Id, input.AnhPath);
        if (row.VideoPath != input.VideoPath) MediaValidation.RequireOwner(user.Id, input.VideoPath);
        (row.CheckInId, row.SoSao, row.NoiDung, row.AnhPath, row.VideoPath) =
            (checkIn, input.SoSao, input.NoiDung, input.AnhPath, input.VideoPath);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/danh-gia"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> DeleteOwnReview(int id, CancellationToken ct)
    {
        await db.DanhGiaDiSans.Where(x => x.DiSanId == id && x.NguoiDungId == user.Id && x.CongKhai).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    private async Task EnsurePublic(int id, CancellationToken ct)
    {
        if (!await db.VwDiSanCongKhais.AnyAsync(x => x.DiSanId == id, ct))
            throw ApiFault.Missing("Di sản không tồn tại.");
    }
}
