using DauChanDiSan.Api.Validation;
using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Shared;
using DauChanDiSan.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Accounts;

public sealed record ProfileInput([Required, StringLength(120)] string HoTen,
    [StringLength(450)] string? AnhDaiDienPath, bool ChoPhepCaNhanHoa);
public sealed record AccountAccessInput(bool HoatDong, bool DoiTac, bool? KiemDuyetVanHoa);

[ApiController, Route("api/tai-khoan"), Authorize]
public sealed class AccountsController(DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user, MediaStorage storage) : ControllerBase
{
    [HttpPost("dong-bo")]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        await sql.RunAsync("dbo.usp_DongBoFirebase", ct, ("FirebaseUid", user.Uid),
            ("Email", user.Email), ("EmailDaXacMinh", user.EmailVerified && user.Email is not null),
            ("HoTenLanDau", user.DisplayName is { Length: > 120 } name ? name[..120] : user.DisplayName));
        return Ok(await ProfileByUid(ct));
    }

    [HttpGet("toi"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> Me(CancellationToken ct) => Ok(await ProfileByUid(ct));

    [HttpPut("toi"), Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> Update(ProfileInput input, CancellationToken ct)
    {
        var row = await db.NguoiDungs.SingleAsync(x => x.NguoiDungId == user.Id, ct);
        if (input.AnhDaiDienPath is { } path && path != row.AnhDaiDienPath)
        {
            MediaValidation.RequireOwner(user.Id, path);
            await storage.EnsureImageExistsAsync(path, ct);
        }
        (row.HoTen, row.AnhDaiDienPath, row.ChoPhepCaNhanHoa) =
            (input.HoTen.Trim(), input.AnhDaiDienPath, input.ChoPhepCaNhanHoa);
        await db.SaveChangesAsync(ct);
        return Ok(await ProfileByUid(ct));
    }

    private async Task<object> ProfileByUid(CancellationToken ct)
    {
        var row = await db.NguoiDungs.AsNoTracking().SingleAsync(x => x.FirebaseUid == user.Uid, ct);
        var roles = await db.NguoiDungVaiTros.Where(x => x.NguoiDungId == row.NguoiDungId)
            .OrderBy(x => x.MaVaiTro).Select(x => x.MaVaiTro).ToListAsync(ct);
        return new { row.NguoiDungId, row.FirebaseUid, row.HoTen, row.Email, row.EmailDaXacMinh,
            row.AnhDaiDienPath, row.ChoPhepCaNhanHoa, vaiTro = roles };
    }
}

[ApiController, Route("api/quan-tri/tai-khoan"), Authorize(Roles = Roles.Admin)]
public sealed class AccountAdminController(DauChanDiSanDbContext db, CurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery, Range(1, 5000)] int trang = 1,
        CancellationToken ct = default)
    {
        var rows = db.NguoiDungs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
            rows = rows.Where(x => x.HoTen.Contains(q) || (x.Email != null && x.Email.Contains(q)));
        return Ok(new { tong = await rows.CountAsync(ct), trang, muc = await rows.OrderBy(x => x.NguoiDungId)
            .Skip((trang - 1) * 30).Take(30).Select(x => new { x.NguoiDungId, x.HoTen, x.Email,
                x.HoatDong, vaiTro = x.NguoiDungVaiTros.Select(r => r.MaVaiTro) }).ToListAsync(ct) });
    }

    [HttpPut("{id:int}/truy-cap")]
    public async Task<IActionResult> UpdateAccess(int id, AccountAccessInput input, CancellationToken ct)
    {
        var row = await db.NguoiDungs.Include(x => x.NguoiDungVaiTros).SingleOrDefaultAsync(x => x.NguoiDungId == id, ct)
            ?? throw ApiFault.Missing("Tài khoản không tồn tại.");
        if (id == user.Id || row.NguoiDungVaiTros.Any(x => x.MaVaiTro == Roles.Admin))
            throw ApiFault.Denied();
        row.HoatDong = input.HoatDong;
        SetRole(Roles.Partner, input.DoiTac);
        if (input.KiemDuyetVanHoa is bool reviewer)
            SetRole(Roles.Culture, reviewer);
        await db.SaveChangesAsync(ct);
        return NoContent();

        void SetRole(string role, bool enabled)
        {
            var current = row.NguoiDungVaiTros.SingleOrDefault(x => x.MaVaiTro == role);
            if (enabled && current is null)
                db.NguoiDungVaiTros.Add(new NguoiDungVaiTro { NguoiDungId = id, MaVaiTro = role });
            if (!enabled && current is not null)
                db.NguoiDungVaiTros.Remove(current);
        }
    }
}
