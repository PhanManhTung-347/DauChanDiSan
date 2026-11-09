using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Extensions;
using DauChanDiSan.Api.Features.Trips.DTOs;
using DauChanDiSan.Api.Helpers;
using DauChanDiSan.Api.Shared;
using DauChanDiSan.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Trips;

[ApiController, Route("api/hanh-trinh"), Authorize(Policy = Roles.Account)]
public sealed class TripsController(
    DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default)
    {
        var query = db.HanhTrinhs.AsNoTracking().Where(x => x.NguoiDungId == user.Id);
        return Ok(await query.OrderByDescending(x => x.HanhTrinhId).ToPageAsync(trang, 30, ct));
    }

    [HttpGet("danh-muc-diem")]
    public async Task<IActionResult> Options([FromQuery] string? loai,
        [FromQuery, StringLength(100)] string? q,
        [FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default)
    {
        if (loai is not ("DI_SAN" or "TRAI_NGHIEM" or "LUU_TRU" or "AN_UONG"))
            throw ApiFault.Invalid("Hãy chọn loại điểm đến.");

        var rows = await sql.QueryAsync("""
            WITH Diem AS (
                SELECT d.DiSanId AS Id, 'DI_SAN' AS Loai, d.Ten, d.DiaChi, d.ViDo, d.KinhDo,
                       d.GiaVeThamKhao AS GiaVnd, 'NGUOI_LUOT' AS DonViTinh,
                       CONVERT(bit, 0) AS LaDuLieuDemo
                FROM dbo.vw_DiSanCongKhai d
                UNION ALL
                SELECT e.TraiNghiemId, 'TRAI_NGHIEM', e.Ten, e.DiaDiemTapTrung, d.ViDo, d.KinhDo,
                       e.GiaMoiNguoi, 'NGUOI_LUOT', CONVERT(bit, 0)
                FROM dbo.vw_TraiNghiemCongKhai e JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId=e.DiSanId
                UNION ALL
                SELECT s.DichVuId, CASE WHEN s.Loai='QUAN_AN' THEN 'AN_UONG' ELSE 'LUU_TRU' END,
                       s.Ten, s.DiaChi, s.ViDo, s.KinhDo, g.GiaVnd, s.DonViTinh, s.LaDuLieuDemo
                FROM dbo.vw_DichVuDuLichCongKhai s
                OUTER APPLY (
                    SELECT TOP (1) GiaVnd FROM dbo.GiaDichVuDuLich
                    WHERE DichVuId=s.DichVuId
                      AND NgayApDung<=CONVERT(date, DATEADD(MINUTE,420,SYSUTCDATETIME()))
                    ORDER BY NgayApDung DESC
                ) g
                WHERE @Demo=1 OR s.LaDuLieuDemo=0
            )
            SELECT Id, Loai, Ten, DiaChi, ViDo, KinhDo, GiaVnd, DonViTinh, LaDuLieuDemo
            FROM Diem
            WHERE Loai=@Loai AND (@Q IS NULL OR CHARINDEX(
                @Q COLLATE Latin1_General_100_CI_AI_SC,
                CONCAT(Ten,N' ',DiaChi) COLLATE Latin1_General_100_CI_AI_SC)>0)
            ORDER BY Ten, Id OFFSET @Skip ROWS FETCH NEXT 31 ROWS ONLY;
            """, ct, ("Loai", loai), ("Q", string.IsNullOrWhiteSpace(q) ? null : q.Trim()),
            ("Demo", environment.IsDevelopment()), ("Skip", (trang - 1) * 30));
        return Ok(new { muc = rows.Take(30), coTrangSau = rows.Count > 30 });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken ct)
    {
        var trip = await db.HanhTrinhs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.HanhTrinhId == id && x.NguoiDungId == user.Id, ct)
            ?? throw ApiFault.Missing("Hành trình không tồn tại.");
        var points = await sql.QueryAsync("""
            SELECT p.DiemHanhTrinhId, p.NgayDuKien, p.ThuTu, p.DiSanId, p.TraiNghiemId, p.DichVuId,
                   p.GioDuKien, p.GhiChu, p.DonGiaDuKienVnd AS DonGiaVnd, p.SoLuongTinhGia, p.DonViTinhGia,
                   COALESCE(d.Ten,e.Ten,s.Ten) AS Ten,
                   CASE WHEN p.DiSanId IS NOT NULL THEN 'DI_SAN'
                        WHEN p.TraiNghiemId IS NOT NULL THEN 'TRAI_NGHIEM'
                        WHEN s.Loai='QUAN_AN' THEN 'AN_UONG' ELSE 'LUU_TRU' END AS Loai,
                   COALESCE(d.DiaChi,e.DiaDiemTapTrung,s.DiaChi) AS DiaChi,
                   COALESCE(d.ViDo,ed.ViDo,s.ViDo) AS ViDo, COALESCE(d.KinhDo,ed.KinhDo,s.KinhDo) AS KinhDo,
                   COALESCE(s.LaDuLieuDemo,CONVERT(bit,0)) AS LaDuLieuDemo
            FROM dbo.DiemHanhTrinh p
            LEFT JOIN dbo.DiSan d ON d.DiSanId=p.DiSanId
            LEFT JOIN dbo.TraiNghiem e ON e.TraiNghiemId=p.TraiNghiemId
            LEFT JOIN dbo.DiSan ed ON ed.DiSanId=e.DiSanId
            LEFT JOIN dbo.DichVuDuLich s ON s.DichVuId=p.DichVuId
            WHERE p.HanhTrinhId=@Id ORDER BY p.NgayDuKien,p.ThuTu;
            """, ct, ("Id", id));
        var checkInIds = await db.HanhTrinhCheckIns.AsNoTracking()
            .Where(x => x.HanhTrinhId == id)
            .OrderBy(x => x.CheckInId).Select(x => x.CheckInId).ToListAsync(ct);
        return Ok(new { hanhTrinh = trip, diem = points, checkInIds });
    }

    [HttpPost]
    public async Task<IActionResult> Create(TripCreateInput input, CancellationToken ct)
    {
        TripPlanValidation.RequireValid(input.NgayBatDau, input.NgayKetThuc, input.Diem);
        string? plan = input.Diem is null ? null : TripPlanJson.Encode(input.Diem);

        // Đề xuất được nạp theo chủ tài khoản. Bản kế hoạch cũ vẫn đọc được.
        if (input.TinNhanAiId is long messageId)
        {
            var saved = await db.TinNhanAis.AsNoTracking()
                .Where(x => x.TinNhanAiid == messageId && x.NguoiDungId == user.Id && x.Vai == "ASSISTANT")
                .Select(x => x.DeXuatHanhTrinhJson).SingleOrDefaultAsync(ct);
            if (saved is null) throw ApiFault.Missing("Không tìm thấy đề xuất của bạn.");
            plan ??= saved;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (plan is not null) await RequirePublicPoints(plan, ct);
        var result = (await sql.RunAsync("dbo.usp_TaoHanhTrinh", ct, ("NguoiDungId", user.Id),
            ("TieuDe", input.TieuDe.Trim()), ("NgayBatDau", input.NgayBatDau), ("NgayKetThuc", input.NgayKetThuc),
            ("MoTa", input.MoTa), ("TinNhanAIId", input.TinNhanAiId), ("KeHoachJson", plan))).Single();
        await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Detail), new { id = result["HanhTrinhId"] }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Edit(long id, TripEditInput input, CancellationToken ct)
    {
        TripPlanValidation.RequireValid(input.NgayBatDau, input.NgayKetThuc, input.Diem);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var row = await db.HanhTrinhs.FromSqlInterpolated(
                $"SELECT * FROM dbo.HanhTrinh WITH (UPDLOCK,HOLDLOCK) WHERE HanhTrinhId={id} AND NguoiDungId={user.Id}")
            .SingleOrDefaultAsync(ct) ?? throw ApiFault.Missing("Hành trình không tồn tại.");
        if (!row.PhienBan.AsSpan().SequenceEqual(input.PhienBan))
            throw new ApiFault(409, "Hành trình đã đổi. Hãy tải lại trước khi sửa.");
        if (row.AnhBiaPath != input.AnhBiaPath) MediaValidation.RequireOwner(user.Id, input.AnhBiaPath);
        if (row.VideoPath != input.VideoPath) MediaValidation.RequireOwner(user.Id, input.VideoPath);

        var plan = input.Diem is null ? null : TripPlanJson.Encode(input.Diem);
        if (plan is not null)
        {
            await RequirePublicPoints(plan, ct);
            await db.DiemHanhTrinhs.Where(x => x.HanhTrinhId == id).ExecuteDeleteAsync(ct);
        }

        (row.TieuDe, row.MoTa, row.NgayBatDau, row.NgayKetThuc, row.TrangThai, row.AnhBiaPath, row.VideoPath) =
            (input.TieuDe.Trim(), input.MoTa, input.NgayBatDau, input.NgayKetThuc, input.TrangThai, input.AnhBiaPath, input.VideoPath);
        db.Entry(row).Property(x => x.TieuDe).IsModified = true;
        await db.SaveChangesAsync(ct);

        if (plan is not null)
        {
            await sql.QueryAsync("""
                INSERT dbo.DiemHanhTrinh (
                    HanhTrinhId,NgayDuKien,ThuTu,DiSanId,TraiNghiemId,GioDuKien,GhiChu,
                    DichVuId,DonGiaDuKienVnd,SoLuongTinhGia,DonViTinhGia)
                SELECT @Id,DATEADD(DAY,x.Ngay-1,@Start),x.ThuTu,x.DiSanId,x.TraiNghiemId,x.Gio,x.GhiChu,
                       x.DichVuId,x.DonGiaVnd,x.SoLuongTinhGia,x.DonViTinhGia
                FROM OPENJSON(@Plan,'$.diem') WITH (
                    Ngay int '$.ngay', ThuTu smallint '$.thuTu', DiSanId int '$.diSanId',
                    TraiNghiemId int '$.traiNghiemId', Gio time(0) '$.gio', GhiChu nvarchar(500) '$.ghiChu',
                    DichVuId int '$.dichVuId', DonGiaVnd decimal(14,0) '$.donGiaVnd',
                    SoLuongTinhGia int '$.soLuongTinhGia', DonViTinhGia varchar(16) '$.donViTinhGia'
                ) x;
                """, ct, ("Plan", plan), ("Id", id), ("Start", input.NgayBatDau));
        }

        await tx.CommitAsync(ct);
        return Ok(new { row.HanhTrinhId, row.PhienBan });
    }

    private async Task RequirePublicPoints(string plan, CancellationToken ct)
    {
        var invalid = await sql.QueryAsync("""
            SELECT TOP (1) 1 AS Invalid
            FROM OPENJSON(@Plan,'$.diem') WITH (
                DiSanId int '$.diSanId', TraiNghiemId int '$.traiNghiemId',
                DichVuId int '$.dichVuId', DonViTinhGia varchar(16) '$.donViTinhGia'
            ) x
            LEFT JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId=x.DiSanId
            LEFT JOIN dbo.vw_TraiNghiemCongKhai e ON e.TraiNghiemId=x.TraiNghiemId
            LEFT JOIN dbo.vw_DichVuDuLichCongKhai s ON s.DichVuId=x.DichVuId
            WHERE (x.DiSanId IS NOT NULL AND d.DiSanId IS NULL)
               OR (x.TraiNghiemId IS NOT NULL AND e.TraiNghiemId IS NULL)
               OR (x.DichVuId IS NOT NULL AND (s.DichVuId IS NULL OR (@Demo=0 AND s.LaDuLieuDemo=1)
                   OR (x.DonViTinhGia IS NOT NULL AND x.DonViTinhGia<>s.DonViTinh)));
            """, ct, ("Plan", plan), ("Demo", environment.IsDevelopment()));
        if (invalid.Count != 0)
            throw ApiFault.Invalid("Có điểm đến đã ẩn, không tồn tại hoặc sai đơn vị tính giá.");
    }
}