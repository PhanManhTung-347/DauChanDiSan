using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DauChanDiSan.Api.Features.Discovery;

[ApiController, AllowAnonymous, Route("api/luu-tru-an-uong")]
public sealed class TravelDirectoryController(SqlProcedures sql, IWebHostEnvironment environment) : ControllerBase
{
    private const int PageSize = 12;

    private const string Visible = """
        FROM dbo.DichVuDuLich d
        JOIN dbo.TinhThanh t ON t.TinhThanhId = d.TinhThanhId
        """;

    private const string PublicFilter = """
        d.TrangThai = 'CONG_KHAI' AND t.HoatDong = 1
        AND d.Loai IN ('KHACH_SAN', 'HOMESTAY', 'QUAN_AN')
        AND (@Demo = 1 OR d.LaDuLieuDemo = 0)
        """;

    private const string Listing = """
        SELECT d.DichVuId, d.Slug, d.Loai, d.Ten, d.DiaChi, d.MoTa,
               d.TinhThanhId, t.Ten AS TinhThanh, d.KhuVuc, d.ViDo, d.KinhDo,
               d.LaDuLieuDemo, a.AnhPath AS AnhBia,
               COALESCE(b.GiaNhoNhat, g.GiaVnd) AS GiaTuVnd,
               CASE WHEN b.GiaNhoNhat IS NOT NULL AND d.Loai = 'QUAN_AN' THEN 'MON'
                    WHEN b.GiaNhoNhat IS NOT NULL THEN 'PHONG_DEM'
                    ELSE d.DonViTinh END AS DonViGia
        """ + "\n" + Visible + "\n" + """
        OUTER APPLY (
            SELECT TOP (1) AnhPath FROM dbo.DichVuHinhAnh a
            WHERE a.DichVuId = d.DichVuId AND a.HoatDong = 1
            ORDER BY a.ThuTu, a.AnhId
        ) a
        OUTER APPLY (
            SELECT MIN(GiaVnd) AS GiaNhoNhat FROM dbo.DichVuBangGiaMuc b
            WHERE b.DichVuId = d.DichVuId AND b.HoatDong = 1
              AND b.LoaiMuc = CASE WHEN d.Loai = 'QUAN_AN' THEN 'MON' ELSE 'PHONG' END
        ) b
        OUTER APPLY (
            SELECT TOP (1) GiaVnd FROM dbo.GiaDichVuDuLich g
            WHERE g.DichVuId = d.DichVuId
              AND g.NgayApDung <= CONVERT(date, DATEADD(MINUTE, 420, SYSUTCDATETIME()))
            ORDER BY g.NgayApDung DESC
        ) g
        WHERE
        """ + "\n" + PublicFilter;

    [HttpGet("bo-loc")]
    public async Task<IActionResult> Filters(CancellationToken ct)
    {
        var rows = await sql.QueryAsync("""
            SELECT DISTINCT d.TinhThanhId, t.Ten AS TinhThanh, d.KhuVuc
            """ + "\n" + Visible + "\nWHERE " + PublicFilter + "\nORDER BY TinhThanh, KhuVuc;",
            ct, ("Demo", environment.IsDevelopment()));

        return Ok(rows.Select(x => new AreaOption(
            (int)x["TinhThanhId"]!, (string)x["TinhThanh"]!, (string?)x["KhuVuc"])));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery, StringLength(16)] string? loai,
        [FromQuery, Range(1, int.MaxValue)] int? tinhThanhId,
        [FromQuery, StringLength(120)] string? khuVuc,
        [FromQuery, StringLength(100)] string? q,
        [FromQuery, Range(typeof(decimal), "0", "99999999999999")] decimal? giaToiDa,
        [FromQuery, StringLength(12)] string? sapXep,
        [FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default)
    {
        if (loai is not (null or "LUU_TRU" or "KHACH_SAN" or "HOMESTAY" or "QUAN_AN"))
            throw ApiFault.Invalid("Loại dịch vụ không hợp lệ.");
        if (sapXep is not (null or "TEN" or "GIA_TANG" or "GIA_GIAM"))
            throw ApiFault.Invalid("Thứ tự sắp xếp không hợp lệ.");

        var rows = await sql.QueryAsync("WITH Listings AS (\n" + Listing + "\n)\n" + """
            SELECT * FROM Listings
            WHERE (@Loai IS NULL OR Loai = @Loai OR (@Loai = 'LUU_TRU' AND Loai IN ('KHACH_SAN', 'HOMESTAY')))
              AND (@Tinh IS NULL OR TinhThanhId = @Tinh)
              AND (@Khu IS NULL OR KhuVuc = @Khu)
              AND (@Max IS NULL OR GiaTuVnd <= @Max)
              AND (@Q IS NULL OR CHARINDEX(
                    CONVERT(nvarchar(100), @Q) COLLATE Latin1_General_100_CI_AI_SC,
                    CONCAT(Ten, N' ', DiaChi, N' ', KhuVuc, N' ', TinhThanh) COLLATE Latin1_General_100_CI_AI_SC
                  ) > 0)
            ORDER BY CASE WHEN @Sort IN ('GIA_TANG', 'GIA_GIAM') AND GiaTuVnd IS NULL THEN 1 ELSE 0 END,
                     CASE WHEN @Sort = 'GIA_TANG' THEN GiaTuVnd END ASC,
                     CASE WHEN @Sort = 'GIA_GIAM' THEN GiaTuVnd END DESC,
                     Ten, DichVuId
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """, ct,
            ("Demo", environment.IsDevelopment()), ("Loai", loai), ("Tinh", tinhThanhId),
            ("Khu", Clean(khuVuc)), ("Q", Clean(q)), ("Max", giaToiDa), ("Sort", sapXep ?? "TEN"),
            ("Skip", (trang - 1) * PageSize), ("Take", PageSize + 1));

        return Ok(new
        {
            muc = rows.Take(PageSize).Select(ToPlace),
            trang,
            coTrangSau = rows.Count > PageSize
        });
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail([StringLength(160)] string slug, CancellationToken ct)
    {
        var rows = await sql.QueryAsync(Listing + "\nAND d.Slug = @Slug;",
            ct, ("Demo", environment.IsDevelopment()), ("Slug", slug));
        if (rows.FirstOrDefault() is not { } row)
            throw ApiFault.Missing("Không tìm thấy địa điểm.");

        var place = ToPlace(row);
        var photos = await sql.QueryAsync("""
            SELECT AnhId, AnhPath, ChuThich FROM dbo.DichVuHinhAnh
            WHERE DichVuId = @Id AND HoatDong = 1
            ORDER BY ThuTu, AnhId;
            """, ct, ("Id", place.DichVuId));

        var prices = await sql.QueryAsync("""
            SELECT MucId, Nhom, Ten, MoTa, GiaVnd, SucChua, AnhPath
            FROM dbo.DichVuBangGiaMuc
            WHERE DichVuId = @Id AND LoaiMuc = @Loai AND HoatDong = 1
            ORDER BY ThuTu, MucId;
            """, ct, ("Id", place.DichVuId), ("Loai", place.Loai == "QUAN_AN" ? "MON" : "PHONG"));

        return Ok(new
        {
            dichVu = place,
            hinhAnh = photos.Select(x => new Photo(
                (int)x["AnhId"]!, (string)x["AnhPath"]!, (string?)x["ChuThich"])),
            bangGia = prices.Select(x => new PriceItem(
                (int)x["MucId"]!, (string?)x["Nhom"], (string)x["Ten"]!, (string?)x["MoTa"],
                (decimal?)x["GiaVnd"], (short?)x["SucChua"], (string?)x["AnhPath"]))
        });
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Place ToPlace(Dictionary<string, object?> x) => new(
        (int)x["DichVuId"]!, (string)x["Slug"]!, (string)x["Loai"]!, (string)x["Ten"]!,
        (string?)x["DiaChi"], (string?)x["MoTa"], (int)x["TinhThanhId"]!, (string)x["TinhThanh"]!,
        (string?)x["KhuVuc"], (decimal?)x["ViDo"], (decimal?)x["KinhDo"],
        (bool)x["LaDuLieuDemo"]!, (string?)x["AnhBia"], (decimal?)x["GiaTuVnd"], (string?)x["DonViGia"]);

    private sealed record Place(int DichVuId, string Slug, string Loai, string Ten, string? DiaChi,
        string? MoTa, int TinhThanhId, string TinhThanh, string? KhuVuc, decimal? ViDo, decimal? KinhDo,
        bool LaDuLieuDemo, string? AnhBia, decimal? GiaTuVnd, string? DonViGia);
    private sealed record AreaOption(int TinhThanhId, string TinhThanh, string? KhuVuc);
    private sealed record Photo(int AnhId, string AnhPath, string? ChuThich);
    private sealed record PriceItem(int MucId, string? Nhom, string Ten, string? MoTa, decimal? GiaVnd,
        short? SucChua, string? AnhPath);
}