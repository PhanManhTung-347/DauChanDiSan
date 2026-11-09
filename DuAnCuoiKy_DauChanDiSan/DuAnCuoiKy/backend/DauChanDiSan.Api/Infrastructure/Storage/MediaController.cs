using DauChanDiSan.Api.Helpers;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Infrastructure.Storage;

[ApiController, Route("api/media")]
public sealed class MediaController(MediaStorage storage, DauChanDiSanDbContext db, CurrentUser user) : ControllerBase
{
    [HttpPost, Authorize(Policy = Roles.Account)]
    [RequestSizeLimit(MediaStorage.Limit + 65536)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        var path = await storage.UploadAsync(user.Id, file, ct);
        return StatusCode(201, new { path, url = "/api/media?path=" + Uri.EscapeDataString(path) });
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string path, CancellationToken ct)
    {
        if (path.Length > 450)
            throw ApiFault.Missing("Media không khả dụng.");
        var owner = user.OptionalId is int id && MediaPaths.BelongsTo(id, path);
        if (!owner && !await IsPublic(path, ct) && !await IsAwaitingReview(path, ct))
            throw ApiFault.Missing("Media không khả dụng.");

        var (content, type) = await storage.DownloadAsync(path, ct);
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(content, type, enableRangeProcessing: true);
    }

    private async Task<bool> IsAwaitingReview(string path, CancellationToken ct) =>
        user.CanReviewHeritage && await db.DongGopDiSans.AnyAsync(x => x.TrangThai == "CHO_DUYET"
            && EF.Functions.Collate(x.AnhPath!, "Latin1_General_100_BIN2") == path, ct);

    private async Task<bool> IsPublic(string path, CancellationToken ct) =>
        await db.NguoiDungs.AnyAsync(x => x.HoatDong && EF.Functions.Collate(x.AnhDaiDienPath!, "Latin1_General_100_BIN2") == path, ct)
        || await db.VwDiSanCongKhais.AnyAsync(x => EF.Functions.Collate(x.AnhBiaPath!, "Latin1_General_100_BIN2") == path || EF.Functions.Collate(x.VideoPath!, "Latin1_General_100_BIN2") == path, ct)
        || await db.MediaDiSans.AnyAsync(x => (EF.Functions.Collate(x.AnhPath!, "Latin1_General_100_BIN2") == path || EF.Functions.Collate(x.VideoPath!, "Latin1_General_100_BIN2") == path)
            && db.VwDiSanCongKhais.Any(d => d.DiSanId == x.DiSanId), ct)
        || await db.VwTraiNghiemCongKhais.AnyAsync(x => EF.Functions.Collate(x.AnhBiaPath!, "Latin1_General_100_BIN2") == path || EF.Functions.Collate(x.VideoPath!, "Latin1_General_100_BIN2") == path, ct)
        || await db.MediaTraiNghiems.AnyAsync(x => (EF.Functions.Collate(x.AnhPath!, "Latin1_General_100_BIN2") == path || EF.Functions.Collate(x.VideoPath!, "Latin1_General_100_BIN2") == path)
            && db.VwTraiNghiemCongKhais.Any(d => d.TraiNghiemId == x.TraiNghiemId), ct)
        || await db.VwNhatKyCongKhais.AnyAsync(x => EF.Functions.Collate(x.AnhBiaPath!, "Latin1_General_100_BIN2") == path || EF.Functions.Collate(x.VideoPath!, "Latin1_General_100_BIN2") == path, ct)
        || await db.MediaNhatKies.AnyAsync(x => (EF.Functions.Collate(x.AnhPath!, "Latin1_General_100_BIN2") == path || EF.Functions.Collate(x.VideoPath!, "Latin1_General_100_BIN2") == path)
            && db.VwNhatKyCongKhais.Any(n => n.MaChiaSe == x.NhatKy.MaChiaSe), ct)
        || await db.DanhGiaDiSans.AnyAsync(x => x.CongKhai && (EF.Functions.Collate(x.AnhPath!, "Latin1_General_100_BIN2") == path || EF.Functions.Collate(x.VideoPath!, "Latin1_General_100_BIN2") == path)
            && db.VwDiSanCongKhais.Any(d => d.DiSanId == x.DiSanId), ct)
        || await db.DanhGiaTraiNghiems.AnyAsync(x => x.CongKhai && (EF.Functions.Collate(x.AnhPath!, "Latin1_General_100_BIN2") == path || EF.Functions.Collate(x.VideoPath!, "Latin1_General_100_BIN2") == path)
            && db.VwTraiNghiemCongKhais.Any(d => d.TraiNghiemId == x.TraiNghiemId), ct)
        || await db.DoiTacs.AnyAsync(x => x.HoatDong
            && EF.Functions.Collate(x.AnhDaiDienPath!, "Latin1_General_100_BIN2") == path
            && db.VwTraiNghiemCongKhais.Any(e => e.DoiTacId == x.DoiTacId), ct)
        || await db.NhiemVus.AnyAsync(x => x.CongKhai && x.HuyHieu != null && EF.Functions.Collate(x.HuyHieu.AnhPath!, "Latin1_General_100_BIN2") == path, ct);
}
