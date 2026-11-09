using DauChanDiSan.Api.Validation;
using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.CheckIns;

public sealed record JournalInput([Required, StringLength(160)] string TieuDe,
    [Required, StringLength(16000)] string NoiDung, DateOnly NgayGhi, long? CheckInId,
    [StringLength(450)] string? AnhBiaPath, [StringLength(450)] string? VideoPath);
public sealed record JournalEditInput([Required, StringLength(160)] string TieuDe,
    [Required, StringLength(16000)] string NoiDung, DateOnly NgayGhi,
    [StringLength(450)] string? AnhBiaPath, [StringLength(450)] string? VideoPath);
public sealed record JournalVisibility(bool CongKhai);

[ApiController, Route("api/so-tay"), Authorize(Policy = Roles.Account)]
public sealed class JournalController(DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user) : ControllerBase
{
    [HttpGet("hanh-trinh/{tripId:long}")]
    public async Task<IActionResult> List(long tripId, CancellationToken ct)
    {
        if (!await db.HanhTrinhs.AnyAsync(x => x.HanhTrinhId == tripId && x.NguoiDungId == user.Id, ct))
            throw ApiFault.Missing("Không tìm thấy hành trình.");
        return Ok(await db.NhatKyHanhTrinhs.AsNoTracking().Where(x => x.HanhTrinhId == tripId)
            .OrderByDescending(x => x.NgayGhi).ThenByDescending(x => x.NhatKyId)
            .Select(x => new { x.NhatKyId, x.TieuDe, x.NgayGhi, x.AnhBiaPath, x.CongKhai, MaChiaSe = x.CongKhai ? (Guid?)x.MaChiaSe : null }).ToListAsync(ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken ct)
    {
        var journal = await Owned(id, ct);
        var media = await db.MediaNhatKies.AsNoTracking().Where(x => x.NhatKyId == id)
            .OrderBy(x => x.ThuTu).ToListAsync(ct);
        return Ok(new { journal, media });
    }

    [HttpPost("hanh-trinh/{tripId:long}")]
    public async Task<IActionResult> Create(long tripId, JournalInput input, CancellationToken ct)
    {
        if (input.NgayGhi == default) throw ApiFault.Invalid("Cần ngày ghi nhật ký.");
        MediaValidation.RequireOwner(user.Id, input.AnhBiaPath, input.VideoPath);
        var result = (await sql.RunAsync("dbo.usp_TaoNhatKy", ct, ("NguoiDungId", user.Id),
            ("HanhTrinhId", tripId), ("TieuDe", input.TieuDe.Trim()), ("NoiDung", input.NoiDung.Trim()),
            ("NgayGhi", input.NgayGhi), ("CheckInId", input.CheckInId), ("AnhBiaPath", input.AnhBiaPath),
            ("VideoPath", input.VideoPath))).Single();
        return CreatedAtAction(nameof(Detail), new { id = result["NhatKyId"] }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Edit(long id, JournalEditInput input, CancellationToken ct)
    {
        var row = await Owned(id, ct);
        if (input.NgayGhi == default) throw ApiFault.Invalid("Cần ngày ghi nhật ký.");
        if (input.AnhBiaPath != row.AnhBiaPath) MediaValidation.RequireOwner(user.Id, input.AnhBiaPath);
        if (input.VideoPath != row.VideoPath) MediaValidation.RequireOwner(user.Id, input.VideoPath);
        (row.TieuDe, row.NoiDung, row.NgayGhi, row.AnhBiaPath, row.VideoPath) =
            (input.TieuDe.Trim(), input.NoiDung.Trim(), input.NgayGhi, input.AnhBiaPath, input.VideoPath);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var row = await Owned(id, ct);
        db.MediaNhatKies.RemoveRange(await db.MediaNhatKies.Where(x => x.NhatKyId == id).ToListAsync(ct));
        db.NhatKyHanhTrinhs.Remove(row);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("{id:long}/chia-se")]
    public async Task<IActionResult> Visibility(long id, JournalVisibility input, CancellationToken ct)
    {
        var row = await Owned(id, ct);
        row.CongKhai = input.CongKhai;
        await db.SaveChangesAsync(ct);
        return Ok(new { row.CongKhai, row.MaChiaSe });
    }

    [HttpPost("{id:long}/media")]
    public async Task<IActionResult> AddMedia(long id, MediaInput input, CancellationToken ct)
    {
        await Owned(id, ct);
        MediaValidation.RequireOne(input.AnhPath, input.VideoPath);
        MediaValidation.RequireOwner(user.Id, input.AnhPath, input.VideoPath);
        var media = new MediaNhatKy { NhatKyId = id, AnhPath = input.AnhPath, VideoPath = input.VideoPath,
            ChuThich = input.ChuThich, ThuTu = input.ThuTu };
        db.MediaNhatKies.Add(media);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { media.MediaId });
    }

    [HttpDelete("{id:long}/media/{mediaId:long}")]
    public async Task<IActionResult> DeleteMedia(long id, long mediaId, CancellationToken ct)
    {
        await Owned(id, ct);
        var media = await db.MediaNhatKies.SingleOrDefaultAsync(x => x.NhatKyId == id && x.MediaId == mediaId, ct)
            ?? throw ApiFault.Missing("Media không tồn tại.");
        db.MediaNhatKies.Remove(media);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("chia-se/{code:guid}"), AllowAnonymous]
    public async Task<IActionResult> Shared(Guid code, CancellationToken ct)
    {
        var journal = await db.VwNhatKyCongKhais.SingleOrDefaultAsync(x => x.MaChiaSe == code, ct)
            ?? throw ApiFault.Missing("Nhật ký không khả dụng.");
        var media = await db.MediaNhatKies.AsNoTracking().Where(x => x.NhatKy.MaChiaSe == code)
            .OrderBy(x => x.ThuTu).Select(x => new { x.AnhPath, x.VideoPath, x.ChuThich, x.ThuTu }).ToListAsync(ct);
        return Ok(new { journal, media });
    }

    private async Task<NhatKyHanhTrinh> Owned(long id, CancellationToken ct) =>
        await db.NhatKyHanhTrinhs.SingleOrDefaultAsync(x => x.NhatKyId == id && x.HanhTrinh.NguoiDungId == user.Id, ct)
        ?? throw ApiFault.Missing("Nhật ký không tồn tại.");
}
