using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DauChanDiSan.Api.Features.Experiences;

public sealed record PartnerApplication([Range(1, int.MaxValue)] int TinhThanhId,
    [Required, StringLength(160)] string Ten, [Required, EmailAddress, StringLength(254)] string Email,
    [Required, RegularExpression(@"[+0-9(). -]{6,24}")] string DienThoai,
    [Required, StringLength(250)] string DiaChi, [Required, StringLength(1000, MinimumLength = 20)] string MoTa);
public sealed record PartnerDecision(bool Duyet, [StringLength(500)] string? LyDo);

[ApiController, Route("api/don-doi-tac"), Authorize(Policy = Roles.Account)]
public sealed class PartnerApplicationsController(SqlProcedures sql, CurrentUser user) : ControllerBase
{
    [HttpGet("toi")]
    public async Task<IActionResult> Mine(CancellationToken ct) => Ok(await sql.QueryAsync("""
        SELECT TOP (30) DonId,Ten,TrangThai,LyDo,TaoLucUtc FROM dbo.DonDangKyDoiTac
        WHERE NguoiDungId=@Id ORDER BY DonId DESC;
        """, ct, ("Id", user.Id)));

    [HttpPost]
    public async Task<IActionResult> Apply(PartnerApplication input, CancellationToken ct)
    {
        if (!user.EmailVerified) throw ApiFault.Invalid("Cần xác minh email trước khi đăng ký đối tác.");
        return StatusCode(201, (await sql.RunAsync("dbo.usp_DangKyDoiTac", ct, ("NguoiDungId", user.Id),
            ("TinhThanhId", input.TinhThanhId), ("Ten", input.Ten.Trim()), ("Email", input.Email.Trim()),
            ("DienThoai", input.DienThoai.Trim()), ("DiaChi", input.DiaChi.Trim()), ("MoTa", input.MoTa.Trim()))).Single());
    }

    [HttpGet("/api/quan-tri/don-doi-tac"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Queue([FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default) =>
        Ok(await sql.QueryAsync("""
            SELECT d.*, n.HoTen AS NguoiGui, t.Ten AS TinhThanh
            FROM dbo.DonDangKyDoiTac d JOIN dbo.NguoiDung n ON n.NguoiDungId=d.NguoiDungId
            JOIN dbo.TinhThanh t ON t.TinhThanhId=d.TinhThanhId
            ORDER BY CASE WHEN d.TrangThai='CHO_DUYET' THEN 0 ELSE 1 END, d.DonId DESC
            OFFSET @Skip ROWS FETCH NEXT 30 ROWS ONLY;
            """, ct, ("Skip", (trang - 1) * 30)));

    [HttpPut("/api/quan-tri/don-doi-tac/{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Decide(int id, PartnerDecision input, CancellationToken ct)
    {
        if (!input.Duyet && string.IsNullOrWhiteSpace(input.LyDo)) throw ApiFault.Invalid("Nhập lý do từ chối.");
        await sql.RunAsync("dbo.usp_DuyetDonDoiTac", ct, ("DonId", id), ("NguoiDuyetId", user.Id),
            ("Duyet", input.Duyet), ("LyDo", input.Duyet ? null : input.LyDo?.Trim()));
        return NoContent();
    }
}
