using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Accounts;

[ApiController, Route("api")]
public sealed class PassportController(DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user) : ControllerBase
{
    [HttpGet("bang-xep-hang")]
    public async Task<IActionResult> Ranking(CancellationToken ct) =>
        Ok(await sql.QueryAsync("SELECT TOP (50) * FROM dbo.vw_XepHangDiSan ORDER BY Hang,NguoiDungId;", ct));

    [HttpGet("tai-khoan/ho-chieu"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var id = user.Id;
        var score = await sql.QueryAsync("SELECT Hang,Diem,SoBai FROM dbo.vw_XepHangDiSan WHERE NguoiDungId=@Id;", ct, ("Id", id));
        var visits = db.CheckInDiSans.AsNoTracking().Where(x => x.NguoiDungId == id);
        var notes = db.NhatKyHanhTrinhs.AsNoTracking().Where(x => x.HanhTrinh.NguoiDungId == id);
        var contributions = db.DongGopDiSans.AsNoTracking().Where(x => x.NguoiDungId == id);
        return Ok(new
        {
            thanhTich = score.SingleOrDefault(),
            tongDiSan = await visits.Select(x => x.DiSanId).Distinct().CountAsync(ct),
            tongKhoanhKhac = await notes.CountAsync(ct),
            tongDongGop = await contributions.CountAsync(ct),
            daDen = await visits.GroupBy(x => new { x.DiSanId, x.DiSan.Ten, x.DiSan.Slug, x.DiSan.TrangThai })
                .Select(x => new { x.Key.DiSanId, x.Key.Ten, slug = x.Key.TrangThai == "CONG_KHAI" ? x.Key.Slug : null,
                    lanCuoiUtc = x.Max(v => v.ThoiGianUtc) }).OrderByDescending(x => x.lanCuoiUtc).Take(8).ToListAsync(ct),
            khoanhKhac = await notes.OrderByDescending(x => x.NgayGhi).ThenByDescending(x => x.NhatKyId).Take(6)
                .Select(x => new { x.NhatKyId, x.HanhTrinhId, x.TieuDe, x.NgayGhi, x.AnhBiaPath }).ToListAsync(ct),
            dongGop = await contributions.OrderByDescending(x => x.TaoLucUtc).Take(6)
                .Select(x => new { x.DongGopId, x.TieuDe, x.TrangThai }).ToListAsync(ct)
        });
    }
}
