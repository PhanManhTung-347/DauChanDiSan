using DauChanDiSan.Api.Extensions;
using DauChanDiSan.Api.Features.Experiences.DTOs;
using DauChanDiSan.Api.Validation;
using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Experiences;

[ApiController, Route("api/trai-nghiem")]
public sealed class ExperiencesController(DauChanDiSanDbContext db, CurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int? diSanId, int? tinhThanhId, [FromQuery, StringLength(100)] string? q,
        [FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default)
    {
        var query = db.VwTraiNghiemCongKhais.AsNoTracking();
        if (diSanId is not null) query = query.Where(x => x.DiSanId == diSanId);
        if (tinhThanhId is not null) query = query.Where(x => x.TinhThanhId == tinhThanhId);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Ten.Contains(q) || x.MoTa.Contains(q));
        return Ok(await query.OrderBy(x => x.Ten).ThenBy(x => x.TraiNghiemId).ToPageAsync(trang, 20, ct));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct)
    {
        var row = await db.VwTraiNghiemCongKhais.SingleOrDefaultAsync(x => x.Slug == slug, ct)
            ?? throw ApiFault.Missing("Trải nghiệm không khả dụng.");
        var now = DateTime.UtcNow;
        var slots = await db.VwLichTraiNghiems.Where(x => x.TraiNghiemId == row.TraiNghiemId && x.TrangThai == "MO"
            && x.BatDauUtc > now).OrderBy(x => x.BatDauUtc).Take(60).ToListAsync(ct);
        var media = await db.MediaTraiNghiems.AsNoTracking().Where(x => x.TraiNghiemId == row.TraiNghiemId)
            .OrderBy(x => x.ThuTu).ToListAsync(ct);
        return Ok(new { traiNghiem = row, lich = slots, media });
    }

    [HttpGet("{id:int}/danh-gia")]
    public async Task<IActionResult> Reviews(int id, [FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default)
    {
        await EnsurePublic(id, ct);
        return Ok(await db.DanhGiaTraiNghiems.AsNoTracking().Where(x => x.TraiNghiemId == id && x.CongKhai)
            .OrderByDescending(x => x.TaoLucUtc).ThenBy(x => x.NguoiDungId).Skip((trang - 1) * 20).Take(20)
            .Select(x => new { x.SoSao, x.NoiDung, x.AnhPath, x.VideoPath, x.TaoLucUtc, tenNguoiViet = x.YeuCauThamGium.NguoiDung.HoTen })
            .ToListAsync(ct));
    }

    [HttpPut("{id:int}/danh-gia"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> Review(int id, ReviewInput input, CancellationToken ct)
    {
        await EnsurePublic(id, ct);
        var request = await db.YeuCauThamGia.Where(x => x.TraiNghiemId == id && x.NguoiDungId == user.Id
                && x.TrangThai == "DA_THAM_GIA").OrderByDescending(x => x.YeuCauId)
            .Select(x => (long?)x.YeuCauId).FirstOrDefaultAsync(ct) ?? throw ApiFault.Denied();

        var row = await db.DanhGiaTraiNghiems.FindAsync([user.Id, id], ct);
        if (row is null)
        {
            row = new DanhGiaTraiNghiem { NguoiDungId = user.Id, TraiNghiemId = id,
                TrangThaiYeuCau = "DA_THAM_GIA", CongKhai = true };
            db.DanhGiaTraiNghiems.Add(row);
        }
        if (row.AnhPath != input.AnhPath) MediaValidation.RequireOwner(user.Id, input.AnhPath);
        if (row.VideoPath != input.VideoPath) MediaValidation.RequireOwner(user.Id, input.VideoPath);
        (row.YeuCauId, row.SoSao, row.NoiDung, row.AnhPath, row.VideoPath) =
            (request, input.SoSao, input.NoiDung, input.AnhPath, input.VideoPath);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/danh-gia"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> DeleteOwnReview(int id, CancellationToken ct)
    {
        await db.DanhGiaTraiNghiems.Where(x => x.TraiNghiemId == id && x.NguoiDungId == user.Id).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    private async Task EnsurePublic(int id, CancellationToken ct)
    {
        if (!await db.VwTraiNghiemCongKhais.AnyAsync(x => x.TraiNghiemId == id, ct))
            throw ApiFault.Missing("Trải nghiệm không khả dụng.");
    }
}

[ApiController, Route("api/yeu-cau-tham-gia"), Authorize(Policy = Roles.Account)]
public sealed class BookingsController(DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(BookingInput input, CancellationToken ct)
    {
        var result = (await sql.RunAsync("dbo.usp_TaoYeuCauThamGia", ct,
            ("NguoiDungId", user.Id), ("LichTraiNghiemId", input.LichTraiNghiemId), ("SoNguoi", input.SoNguoi),
            ("MaYeuCau", input.MaYeuCau), ("GhiChu", input.GhiChu))).Single();
        return Ok(result);
    }

    [HttpGet("toi")]
    public Task<List<BookingDto>> Mine([Range(1, 5000)] int trang = 1, CancellationToken ct = default) =>
        ReadBookings().Skip((trang - 1) * 30).Take(30).ToListAsync(ct);

    [HttpGet("{id:long}")]
    public async Task<BookingDto> Detail(long id, CancellationToken ct) =>
        await ReadBookings(id).SingleOrDefaultAsync(ct)
        ?? throw ApiFault.Missing("Không tìm thấy yêu cầu tham gia.");

    private IQueryable<BookingDto> ReadBookings(long? id = null) =>
        from b in db.YeuCauThamGia.AsNoTracking()
        join e in db.TraiNghiems on b.TraiNghiemId equals e.TraiNghiemId
        where b.NguoiDungId == user.Id && (id == null || b.YeuCauId == id)
        orderby b.YeuCauId descending
        select new BookingDto(b.YeuCauId, b.TraiNghiemId, e.Ten, b.LichTraiNghiemId,
            b.LichTraiNghiem.BatDauUtc, b.SoNguoi, b.DonGiaDuKien, b.TrangThai, b.GhiChu, b.TaoLucUtc);

    [HttpPut("{id:long}/trang-thai")]
    public async Task<IActionResult> Status(long id, BookingStatusInput input, CancellationToken ct)
    {
        var result = (await sql.RunAsync("dbo.usp_DoiTrangThaiYeuCau", ct, ("NguoiXuLyId", user.Id),
            ("YeuCauId", id), ("TrangThaiMoi", input.TrangThaiMoi))).Single();
        return Ok(result);
    }
}
