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

[ApiController, Route("api/quan-tri"), Authorize(Roles = Roles.Admin)]
public sealed class ExperienceAdminController(DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user) : ControllerBase
{
    [HttpGet("trai-nghiem")]
    public async Task<IActionResult> Experiences(string? trangThai, [FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default)
    {
        var query = db.TraiNghiems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(x => x.TrangThai == trangThai);
        return Ok(await query.OrderByDescending(x => x.TraiNghiemId).Skip((trang - 1) * 30).Take(30).ToListAsync(ct));
    }

    [HttpPut("trai-nghiem/{id:int}/cong-khai")]
    public async Task<IActionResult> Publish(int id, PublishInput input, CancellationToken ct)
    {
        await sql.RunAsync("dbo.usp_DuyetTraiNghiem", ct, ("QuanTriId", user.Id),
            ("TraiNghiemId", id), ("CongKhai", input.CongKhai));
        return NoContent();
    }

    [HttpGet("trai-nghiem/{id:int}/danh-gia")]
    public async Task<IActionResult> ReviewQueue(int id, [Range(1, 5000)] int trang = 1, CancellationToken ct = default) =>
        Ok(await db.DanhGiaTraiNghiems.AsNoTracking().Where(x => x.TraiNghiemId == id)
            .OrderByDescending(x => x.TaoLucUtc).ThenBy(x => x.NguoiDungId).ToPageAsync(trang, 20,
                x => new ReviewModerationDto(x.NguoiDungId, x.SoSao, x.NoiDung, x.CongKhai, x.TaoLucUtc), ct));

    [HttpPut("trai-nghiem/{id:int}/danh-gia/{accountId:int}/cong-khai")]
    public async Task<IActionResult> ModerateReview(int id, int accountId, PublishInput input, CancellationToken ct)
    {
        var review = await db.DanhGiaTraiNghiems.FindAsync([accountId, id], ct)
            ?? throw ApiFault.Missing("Không tìm thấy đánh giá.");
        review.CongKhai = input.CongKhai;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("doi-tac")]
    [HttpPut("doi-tac/{id:int}")]
    public async Task<IActionResult> SavePartner(int? id, PartnerInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.DienThoai) && string.IsNullOrWhiteSpace(input.Email))
            throw ApiFault.Invalid("Cần ít nhất số điện thoại hoặc email.");
        var row = id is int p ? await db.DoiTacs.FindAsync([p], ct)
            ?? throw ApiFault.Missing("Đối tác không tồn tại.") : new DoiTac();
        if (row.AnhDaiDienPath != input.AnhDaiDienPath) MediaValidation.RequireOwner(user.Id, input.AnhDaiDienPath);
        (row.TinhThanhId, row.Ten, row.MoTa, row.DienThoai, row.Email, row.DiaChi, row.AnhDaiDienPath, row.HoatDong) =
            (input.TinhThanhId, input.Ten.Trim(), input.MoTa, input.DienThoai, input.Email, input.DiaChi, input.AnhDaiDienPath, input.HoatDong);
        if (id is null) db.DoiTacs.Add(row);
        await db.SaveChangesAsync(ct);
        return Ok(new { row.DoiTacId });
    }

    [HttpGet("doi-tac/{id:int}/thanh-vien")]
    public async Task<IActionResult> Members(int id, CancellationToken ct) =>
        Ok(await db.ThanhVienDoiTacs.AsNoTracking().Where(x => x.DoiTacId == id).OrderBy(x => x.NguoiDungId)
            .Select(x => new { x.NguoiDungId, x.NguoiDung.HoTen, x.Quyen, x.HoatDong }).ToListAsync(ct));

    [HttpPut("doi-tac/{id:int}/thanh-vien/{accountId:int}")]
    public async Task<IActionResult> SaveMember(int id, int accountId, PartnerMemberInput input, CancellationToken ct)
    {
        if (!await db.NguoiDungVaiTros.AnyAsync(x => x.NguoiDungId == accountId && x.MaVaiTro == Roles.Partner, ct))
            throw ApiFault.Invalid("Tài khoản cần được cấp vai trò DOI_TAC trước.");
        var row = await db.ThanhVienDoiTacs.FindAsync([id, accountId], ct);
        if (row is null)
        {
            row = new ThanhVienDoiTac { DoiTacId = id, NguoiDungId = accountId };
            db.ThanhVienDoiTacs.Add(row);
        }
        (row.Quyen, row.HoatDong) = (input.Quyen, input.HoatDong);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
