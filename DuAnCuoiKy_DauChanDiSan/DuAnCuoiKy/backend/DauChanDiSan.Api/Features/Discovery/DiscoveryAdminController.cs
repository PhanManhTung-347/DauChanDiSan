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

public sealed record HeritageInput(int TinhThanhId, int LoaiDiSanId,
    [Required, StringLength(160), RegularExpression("[a-z0-9]+(?:-[a-z0-9]+)*")] string Slug,
    [Required, StringLength(200)] string Ten, [Required, StringLength(1000)] string TomTat,
    [StringLength(20000)] string? NoiDung, [Required, StringLength(250)] string DiaChi,
    [Range(-90, 90)] decimal ViDo, [Range(-180, 180)] decimal KinhDo,
    [Range(10, 3000)] short BanKinhCheckInM, [Range(5, 500)] short SaiSoGpsToiDaM,
    [Range(typeof(decimal), "0", "999999999999")] decimal? GiaVeThamKhao,
    [Range(1, 1440)] short? ThoiLuongPhut, [StringLength(450)] string? AnhBiaPath,
    [StringLength(450)] string? VideoPath, [Required] string TrangThai,
    [MaxLength(30)] int[]? ChuDeIds);
public sealed record OpeningInput([Range(1, 7)] byte ThuTrongTuan, TimeOnly GioMo, TimeOnly GioDong);
public sealed record SourceInput([Required, StringLength(200)] string TieuDe,
    [Required, StringLength(1600)] string TrichYeu, [Required, StringLength(200)] string Nguon,
    [StringLength(1000), HttpUrl] string? UrlNguon);
public sealed record CatalogInput([Required, StringLength(32)] string Ma, [Required, StringLength(100)] string Ten,
    [StringLength(500)] string? MoTa, bool HoatDong = true);

[ApiController, Route("api/quan-tri/di-san"), Authorize(Roles = Roles.HeritageModerators)]
public sealed class DiscoveryAdminController(DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default) =>
        Ok(await db.DiSans.AsNoTracking().OrderByDescending(x => x.DiSanId).ToPageAsync(trang, 30, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var row = await db.DiSans.AsNoTracking().SingleOrDefaultAsync(x => x.DiSanId == id, ct)
            ?? throw ApiFault.Missing("Di sản không tồn tại.");
        return Ok(new { diSan = row,
            chuDeIds = await db.DiSans.Where(x => x.DiSanId == id).SelectMany(x => x.ChuDes)
                .Select(x => x.ChuDeId).ToListAsync(ct),
            gioThamQuan = await db.GioThamQuans.AsNoTracking().Where(x => x.DiSanId == id).ToListAsync(ct),
            tuLieu = await db.TuLieuDiSans.AsNoTracking().Where(x => x.DiSanId == id).ToListAsync(ct),
            media = await db.MediaDiSans.AsNoTracking().Where(x => x.DiSanId == id).ToListAsync(ct) });
    }

    [HttpPost]
    public async Task<IActionResult> Create(HeritageInput input, CancellationToken ct)
    {
        var row = new DiSan();
        await Apply(row, input, ct);
        db.DiSans.Add(row);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Detail), new { id = row.DiSanId }, new { row.DiSanId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Edit(int id, HeritageInput input, CancellationToken ct)
    {
        var row = await db.DiSans.Include(x => x.ChuDes).SingleOrDefaultAsync(x => x.DiSanId == id, ct)
            ?? throw ApiFault.Missing("Di sản không tồn tại.");
        await Apply(row, input, ct);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/gio-tham-quan")]
    [HttpPut("{id:int}/gio-tham-quan/{hourId:int}")]
    public async Task<IActionResult> SaveHours(int id, int? hourId, OpeningInput input, CancellationToken ct)
    {
        await EnsureHeritage(id, ct);
        if (input.GioDong <= input.GioMo) throw ApiFault.Invalid("Giờ đóng phải sau giờ mở.");
        var row = hourId is int key
            ? await db.GioThamQuans.SingleOrDefaultAsync(x => x.DiSanId == id && x.GioThamQuanId == key, ct)
                ?? throw ApiFault.Missing("Không tìm thấy giờ tham quan.")
            : new GioThamQuan { DiSanId = id };
        (row.ThuTrongTuan, row.GioMo, row.GioDong) = (input.ThuTrongTuan, input.GioMo, input.GioDong);
        if (hourId is null) db.GioThamQuans.Add(row);
        await db.SaveChangesAsync(ct);
        return StatusCode(hourId is null ? 201 : 200, new { row.GioThamQuanId });
    }

    [HttpDelete("{id:int}/gio-tham-quan/{hourId:int}")]
    public async Task<IActionResult> DeleteHours(int id, int hourId, CancellationToken ct)
    {
        await db.GioThamQuans.Where(x => x.DiSanId == id && x.GioThamQuanId == hourId).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/tu-lieu")]
    public async Task<IActionResult> AddSource(int id, SourceInput input, CancellationToken ct)
    {
        await EnsureHeritage(id, ct);
        var row = new TuLieuDiSan { DiSanId = id, TieuDe = input.TieuDe, TrichYeu = input.TrichYeu,
            Nguon = input.Nguon, UrlNguon = input.UrlNguon };
        db.TuLieuDiSans.Add(row);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { row.TuLieuId });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("tu-lieu/{id:int}/cong-khai")]
    public async Task<IActionResult> PublishSource(int id, PublishInput input, CancellationToken ct)
    {
        await sql.RunAsync("dbo.usp_DuyetTuLieu", ct, ("QuanTriId", user.Id),
            ("TuLieuId", id), ("CongKhai", input.CongKhai));
        return NoContent();
    }

    [HttpGet("{id:int}/danh-gia")]
    public async Task<IActionResult> ReviewQueue(int id, [Range(1, 5000)] int trang = 1, CancellationToken ct = default) =>
        Ok(await db.DanhGiaDiSans.AsNoTracking().Where(x => x.DiSanId == id)
            .OrderByDescending(x => x.TaoLucUtc).ThenBy(x => x.NguoiDungId).ToPageAsync(trang, 20,
                x => new ReviewModerationDto(x.NguoiDungId, x.SoSao, x.NoiDung, x.CongKhai, x.TaoLucUtc), ct));

    [HttpPut("{id:int}/danh-gia/{accountId:int}/cong-khai")]
    public async Task<IActionResult> ModerateReview(int id, int accountId, PublishInput input, CancellationToken ct)
    {
        var review = await db.DanhGiaDiSans.FindAsync([accountId, id], ct)
            ?? throw ApiFault.Missing("Không tìm thấy đánh giá.");
        review.CongKhai = input.CongKhai;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/media")]
    public async Task<IActionResult> AddMedia(int id, MediaInput input, CancellationToken ct)
    {
        await EnsureHeritage(id, ct);
        MediaValidation.RequireOne(input.AnhPath, input.VideoPath);
        MediaValidation.RequireOwner(user.Id, input.AnhPath, input.VideoPath);
        var row = new MediaDiSan { DiSanId = id, AnhPath = input.AnhPath, VideoPath = input.VideoPath,
            ChuThich = input.ChuThich, ThuTu = input.ThuTu };
        db.MediaDiSans.Add(row);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { row.MediaId });
    }

    [HttpDelete("{id:int}/media/{mediaId:int}")]
    public async Task<IActionResult> DeleteMedia(int id, int mediaId, CancellationToken ct)
    {
        await db.MediaDiSans.Where(x => x.DiSanId == id && x.MediaId == mediaId).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("danh-muc/{loai}")]
    [HttpPut("danh-muc/{loai}/{id:int}")]
    public async Task<IActionResult> SaveCatalog(string loai, int? id, CatalogInput input, CancellationToken ct)
    {
        var ma = input.Ma.Trim();
        var ten = input.Ten.Trim();
        switch (loai)
        {
            case "tinh-thanh":
                var tinh = id is int t ? await db.TinhThanhs.FindAsync([t], ct)
                    ?? throw ApiFault.Missing("Không tìm thấy tỉnh thành.") : new TinhThanh();
                (tinh.Ma, tinh.Ten, tinh.HoatDong) = (ma, ten, input.HoatDong);
                if (id is null) db.TinhThanhs.Add(tinh);
                await db.SaveChangesAsync(ct);
                return Ok(new { id = tinh.TinhThanhId });
            case "loai-di-san":
                var type = id is int l ? await db.LoaiDiSans.FindAsync([l], ct)
                    ?? throw ApiFault.Missing("Không tìm thấy loại di sản.") : new LoaiDiSan();
                (type.Ma, type.Ten, type.MoTa) = (ma, ten, input.MoTa);
                if (id is null) db.LoaiDiSans.Add(type);
                await db.SaveChangesAsync(ct);
                return Ok(new { id = type.LoaiDiSanId });
            case "chu-de":
                var tag = id is int c ? await db.ChuDes.FindAsync([c], ct)
                    ?? throw ApiFault.Missing("Không tìm thấy chủ đề.") : new ChuDe();
                (tag.Ma, tag.Ten) = (ma, ten);
                if (id is null) db.ChuDes.Add(tag);
                await db.SaveChangesAsync(ct);
                return Ok(new { id = tag.ChuDeId });
            default: throw ApiFault.Invalid("Danh mục không được hỗ trợ.");
        }
    }

    private async Task Apply(DiSan row, HeritageInput input, CancellationToken ct)
    {
        if (input.TrangThai is not ("NHAP" or "CONG_KHAI" or "AN"))
            throw ApiFault.Invalid("Trạng thái di sản không hợp lệ.");
        if (row.AnhBiaPath != input.AnhBiaPath) MediaValidation.RequireOwner(user.Id, input.AnhBiaPath);
        if (row.VideoPath != input.VideoPath) MediaValidation.RequireOwner(user.Id, input.VideoPath);
        (row.TinhThanhId, row.LoaiDiSanId, row.Slug, row.Ten, row.TomTat, row.NoiDung, row.DiaChi) =
            (input.TinhThanhId, input.LoaiDiSanId, input.Slug, input.Ten.Trim(), input.TomTat.Trim(), input.NoiDung, input.DiaChi.Trim());
        (row.ViDo, row.KinhDo, row.BanKinhCheckInM, row.SaiSoGpsToiDaM, row.GiaVeThamKhao, row.ThoiLuongPhut) =
            (input.ViDo, input.KinhDo, input.BanKinhCheckInM, input.SaiSoGpsToiDaM, input.GiaVeThamKhao, input.ThoiLuongPhut);
        (row.AnhBiaPath, row.VideoPath, row.TrangThai) = (input.AnhBiaPath, input.VideoPath, input.TrangThai);
        if (input.ChuDeIds is not null)
        {
            var ids = input.ChuDeIds.Distinct().ToArray();
            var topics = await db.ChuDes.Where(x => ids.Contains(x.ChuDeId)).ToListAsync(ct);
            if (topics.Count != ids.Length) throw ApiFault.Invalid("Có chủ đề không tồn tại.");
            row.ChuDes.Clear();
            foreach (var topic in topics) row.ChuDes.Add(topic);
        }
    }

    private async Task EnsureHeritage(int id, CancellationToken ct)
    {
        if (!await db.DiSans.AnyAsync(x => x.DiSanId == id, ct))
            throw ApiFault.Missing("Di sản không tồn tại.");
    }
}
