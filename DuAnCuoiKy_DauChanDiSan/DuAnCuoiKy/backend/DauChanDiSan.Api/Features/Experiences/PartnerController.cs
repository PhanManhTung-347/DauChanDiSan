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

[ApiController, Route("api/doi-tac"), Authorize(Roles = Roles.Partner + "," + Roles.Admin)]
public sealed class PartnerController(DauChanDiSanDbContext db, CurrentUser user) : ControllerBase
{
    [HttpGet("toi")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var query = db.DoiTacs.AsNoTracking();
        if (!user.Admin)
            query = query.Where(x => x.HoatDong && x.ThanhVienDoiTacs.Any(m => m.NguoiDungId == user.Id && m.HoatDong));
        return Ok(await query.OrderBy(x => x.Ten).Take(100).ToListAsync(ct));
    }

    [HttpGet("{id:int}/trai-nghiem")]
    public async Task<IActionResult> Experiences(int id, CancellationToken ct)
    {
        await RequirePartner(id, false, ct);
        return Ok(await db.TraiNghiems.AsNoTracking().Where(x => x.DoiTacId == id)
            .OrderByDescending(x => x.TraiNghiemId).Take(100).ToListAsync(ct));
    }

    [HttpPost("{id:int}/trai-nghiem")]
    public async Task<IActionResult> CreateExperience(int id, ExperienceInput input, CancellationToken ct)
    {
        await RequirePartner(id, true, ct);
        var row = new TraiNghiem { DoiTacId = id };
        Apply(row, input);
        db.TraiNghiems.Add(row);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { row.TraiNghiemId, row.TrangThai });
    }

    [HttpPut("{id:int}/trai-nghiem/{experienceId:int}")]
    public async Task<IActionResult> EditExperience(int id, int experienceId, ExperienceInput input, CancellationToken ct)
    {
        await RequirePartner(id, true, ct);
        var row = await FindExperience(id, experienceId, ct);
        Apply(row, input);
        await db.SaveChangesAsync(ct);
        return Ok(new { row.TraiNghiemId, row.TrangThai });
    }

    [HttpGet("{id:int}/trai-nghiem/{experienceId:int}/lich")]
    public async Task<IActionResult> Slots(int id, int experienceId, CancellationToken ct)
    {
        await RequirePartner(id, false, ct);
        await FindExperience(id, experienceId, ct);
        return Ok(await db.LichTraiNghiems.AsNoTracking().Where(x => x.TraiNghiemId == experienceId)
            .OrderByDescending(x => x.BatDauUtc).Take(100).ToListAsync(ct));
    }

    [HttpPost("{id:int}/trai-nghiem/{experienceId:int}/lich")]
    [HttpPut("{id:int}/trai-nghiem/{experienceId:int}/lich/{slotId:int}")]
    public async Task<IActionResult> SaveSlot(int id, int experienceId, int? slotId, SlotInput input, CancellationToken ct)
    {
        await RequirePartner(id, true, ct);
        await FindExperience(id, experienceId, ct);
        if (input.BatDauUtc == default || input.KetThucUtc <= input.BatDauUtc)
            throw ApiFault.Invalid("Thời gian hoặc trạng thái lịch không hợp lệ.");
        var row = slotId is int slot ? await db.LichTraiNghiems.SingleOrDefaultAsync(x =>
            x.LichTraiNghiemId == slot && x.TraiNghiemId == experienceId, ct)
            ?? throw ApiFault.Missing("Lịch không tồn tại.") : new LichTraiNghiem { TraiNghiemId = experienceId };
        (row.BatDauUtc, row.KetThucUtc, row.SucChua, row.TrangThai) =
            (input.BatDauUtc.UtcDateTime, input.KetThucUtc.UtcDateTime, input.SucChua, input.TrangThai);
        if (slotId is null) db.LichTraiNghiems.Add(row);
        await db.SaveChangesAsync(ct);
        return Ok(new { row.LichTraiNghiemId });
    }

    [HttpGet("{id:int}/trai-nghiem/{experienceId:int}/media")]
    public async Task<IActionResult> Media(int id, int experienceId, CancellationToken ct)
    {
        await RequirePartner(id, false, ct);
        await FindExperience(id, experienceId, ct);
        return Ok(await db.MediaTraiNghiems.AsNoTracking().Where(x => x.TraiNghiemId == experienceId)
            .OrderBy(x => x.ThuTu).ToListAsync(ct));
    }

    [HttpPost("{id:int}/trai-nghiem/{experienceId:int}/media")]
    public async Task<IActionResult> AddMedia(int id, int experienceId, MediaInput input, CancellationToken ct)
    {
        await RequirePartner(id, true, ct);
        await FindExperience(id, experienceId, ct);
        MediaValidation.RequireOne(input.AnhPath, input.VideoPath);
        MediaValidation.RequireOwner(user.Id, input.AnhPath, input.VideoPath);
        var row = new MediaTraiNghiem { TraiNghiemId = experienceId, AnhPath = input.AnhPath,
            VideoPath = input.VideoPath, ChuThich = input.ChuThich, ThuTu = input.ThuTu };
        db.MediaTraiNghiems.Add(row);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { row.MediaId });
    }

    [HttpDelete("{id:int}/trai-nghiem/{experienceId:int}/media/{mediaId:int}")]
    public async Task<IActionResult> DeleteMedia(int id, int experienceId, int mediaId, CancellationToken ct)
    {
        await RequirePartner(id, true, ct);
        await FindExperience(id, experienceId, ct);
        await db.MediaTraiNghiems.Where(x => x.TraiNghiemId == experienceId && x.MediaId == mediaId).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:int}/yeu-cau")]
    public async Task<IActionResult> Requests(int id, [FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default)
    {
        await RequirePartner(id, false, ct);
        return Ok(await (from b in db.YeuCauThamGia.AsNoTracking()
            join e in db.TraiNghiems on b.TraiNghiemId equals e.TraiNghiemId
            where e.DoiTacId == id orderby b.YeuCauId descending
            select new { b.YeuCauId, b.LichTraiNghiemId, b.TraiNghiemId, tenTraiNghiem = e.Ten, b.NguoiDung.HoTen,
                b.SoNguoi, b.GhiChu, b.TrangThai, b.LichTraiNghiem.BatDauUtc }).Skip((trang - 1) * 30).Take(30).ToListAsync(ct));
    }

    private async Task RequirePartner(int id, bool manager, CancellationToken ct)
    {
        if (!await db.DoiTacs.AnyAsync(x => x.DoiTacId == id && (user.Admin ||
            (x.HoatDong && x.ThanhVienDoiTacs.Any(m => m.HoatDong && m.NguoiDungId == user.Id && (!manager || m.Quyen == "QUAN_LY")))), ct))
            throw ApiFault.Denied();
    }

    private async Task<TraiNghiem> FindExperience(int id, int experienceId, CancellationToken ct) =>
        await db.TraiNghiems.SingleOrDefaultAsync(x => x.TraiNghiemId == experienceId && x.DoiTacId == id, ct)
        ?? throw ApiFault.Missing("Trải nghiệm không thuộc đối tác.");

    private void Apply(TraiNghiem row, ExperienceInput input)
    {
        if (row.AnhBiaPath != input.AnhBiaPath) MediaValidation.RequireOwner(user.Id, input.AnhBiaPath);
        if (row.VideoPath != input.VideoPath) MediaValidation.RequireOwner(user.Id, input.VideoPath);
        (row.DiSanId, row.Slug, row.Ten, row.MoTa, row.DiaDiemTapTrung) =
            (input.DiSanId, input.Slug, input.Ten.Trim(), input.MoTa.Trim(), input.DiaDiemTapTrung);
        (row.ThoiLuongPhut, row.GiaMoiNguoi, row.AnhBiaPath, row.VideoPath) =
            (input.ThoiLuongPhut, input.GiaMoiNguoi, input.AnhBiaPath, input.VideoPath);
        (row.TrangThai, row.NguoiDuyetId, row.DuyetLucUtc) =
            (input.GuiDuyet ? "CHO_DUYET" : "NHAP", (int?)null, (DateTime?)null);
    }
}
