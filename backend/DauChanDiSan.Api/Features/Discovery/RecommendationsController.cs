using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Validation;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Discovery;

public sealed record PreferenceInput([Range(1, int.MaxValue)] int ChuDeId, [Range(1, 5)] byte MucDo);
public sealed record EventInput([NonEmptyGuid] Guid MaSuKien, [Required, OneOf("XEM", "NHAN_GOI_Y", "AN_GOI_Y")] string Loai,
    int? DiSanId, int? TraiNghiemId, long? LuotGoiYId, short? ThuHang);

[ApiController, Route("api/goi-y")]
public sealed class RecommendationsController(RecommendationService recommendations, SqlProcedures sql,
    DauChanDiSanDbContext db, CurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int? tinhThanhId, [FromQuery, Range(1, 30)] int gioiHan = 10,
        [FromQuery] string viTri = "TRANG_CHU", CancellationToken ct = default)
    {
        if (viTri is not ("TRANG_CHU" or "CHI_TIET" or "HANH_TRINH"))
            throw ApiFault.Invalid("Vị trí hiển thị không hợp lệ.");
        Response.Headers.CacheControl = "private, no-store";
        return Ok(await recommendations.SuggestAsync(user.OptionalId, tinhThanhId, gioiHan, viTri, ct));
    }

    [HttpGet("so-thich"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> Preferences(CancellationToken ct) =>
        Ok(await db.SoThichNguoiDungs.AsNoTracking().Where(x => x.NguoiDungId == user.Id)
            .OrderBy(x => x.ChuDeId).Select(x => new { x.ChuDeId, x.MucDo }).ToListAsync(ct));

    [HttpPut("so-thich"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> UpdatePreferences([FromBody, MaxLength(30)] List<PreferenceInput> input, CancellationToken ct)
    {
        if (input.Any(x => x is null) || input.Select(x => x.ChuDeId).Distinct().Count() != input.Count)
            throw ApiFault.Invalid("Mỗi chủ đề chỉ xuất hiện một lần.");
        var ids = input.Select(x => x.ChuDeId).ToArray();
        if (await db.ChuDes.CountAsync(x => ids.Contains(x.ChuDeId), ct) != ids.Length)
            throw ApiFault.Invalid("Có chủ đề không tồn tại.");
        var old = await db.SoThichNguoiDungs.Where(x => x.NguoiDungId == user.Id).ToListAsync(ct);
        var requested = input.ToDictionary(x => x.ChuDeId, x => x.MucDo);
        foreach (var row in old)
        {
            if (requested.Remove(row.ChuDeId, out var weight)) row.MucDo = weight;
            else db.SoThichNguoiDungs.Remove(row);
        }
        db.SoThichNguoiDungs.AddRange(requested.Select(x => new SoThichNguoiDung
        {
            NguoiDungId = user.Id, ChuDeId = x.Key, MucDo = x.Value
        }));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("tuong-tac"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> Event(EventInput input, CancellationToken ct)
    {
        await sql.RunAsync("dbo.usp_GhiSuKienTuongTac", ct, ("NguoiDungId", user.Id), ("MaSuKien", input.MaSuKien),
            ("Loai", input.Loai), ("DiSanId", input.DiSanId), ("TraiNghiemId", input.TraiNghiemId),
            ("LuotGoiYId", input.LuotGoiYId), ("ThuHang", input.ThuHang));
        return NoContent();
    }
}
