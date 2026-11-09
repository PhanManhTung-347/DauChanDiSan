using DauChanDiSan.Api.Extensions;
using DauChanDiSan.Api.Features.Trips.DTOs;
using DauChanDiSan.Api.Helpers;
using DauChanDiSan.Api.Validation;
using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Trips;

[ApiController, Route("api/hanh-trinh"), Authorize(Policy = Roles.Account)]
public sealed class TripsController(DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery, Range(1, 5000)] int trang = 1, CancellationToken ct = default)
    {
        var query = db.HanhTrinhs.AsNoTracking().Where(x => x.NguoiDungId == user.Id);
        return Ok(await query.OrderByDescending(x => x.HanhTrinhId).ToPageAsync(trang, 30, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken ct)
    {
        var trip = await db.HanhTrinhs.AsNoTracking().SingleOrDefaultAsync(x => x.HanhTrinhId == id && x.NguoiDungId == user.Id, ct)
            ?? throw ApiFault.Missing("Hành trình không tồn tại.");
        var points = await db.DiemHanhTrinhs.AsNoTracking().Where(x => x.HanhTrinhId == id)
            .OrderBy(x => x.NgayDuKien).ThenBy(x => x.ThuTu)
            .Select(x => new
            {
                x.DiemHanhTrinhId,
                x.NgayDuKien,
                x.ThuTu,
                x.DiSanId,
                x.TraiNghiemId,
                x.GioDuKien,
                x.GhiChu,
                ten = x.DiSanId != null ? x.DiSan!.Ten : x.TraiNghiem!.Ten,
                viDo = x.DiSanId != null ? x.DiSan!.ViDo : x.TraiNghiem!.DiSan.ViDo,
                kinhDo = x.DiSanId != null ? x.DiSan!.KinhDo : x.TraiNghiem!.DiSan.KinhDo
            })
            .ToListAsync(ct);
        var checkIns = await db.HanhTrinhCheckIns.AsNoTracking().Where(x => x.HanhTrinhId == id)
            .OrderBy(x => x.CheckInId).Select(x => x.CheckInId).ToListAsync(ct);
        return Ok(new { hanhTrinh = trip, diem = points, checkInIds = checkIns });
    }

    [HttpPost]
    public async Task<IActionResult> Create(TripCreateInput input, CancellationToken ct)
    {
        TripPlanValidation.RequireValid(input.NgayBatDau, input.NgayKetThuc, input.Diem);
        var result = (await sql.RunAsync("dbo.usp_TaoHanhTrinh", ct, ("NguoiDungId", user.Id),
            ("TieuDe", input.TieuDe.Trim()), ("NgayBatDau", input.NgayBatDau), ("NgayKetThuc", input.NgayKetThuc),
            ("MoTa", input.MoTa), ("TinNhanAIId", input.TinNhanAiId),
            ("KeHoachJson", input.Diem is null ? null : TripPlanJson.Encode(input.Diem)))).Single();
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
        if (input.Diem is not null)
        {
            var heritageIds = input.Diem.Where(x => x.DiSanId.HasValue).Select(x => x.DiSanId!.Value).Distinct().ToArray();
            var experienceIds = input.Diem.Where(x => x.TraiNghiemId.HasValue).Select(x => x.TraiNghiemId!.Value).Distinct().ToArray();
            if (await db.VwDiSanCongKhais.CountAsync(x => heritageIds.Contains(x.DiSanId), ct) != heritageIds.Length ||
                await db.VwTraiNghiemCongKhais.CountAsync(x => experienceIds.Contains(x.TraiNghiemId), ct) != experienceIds.Length)
                throw ApiFault.Invalid("Có điểm đến không tồn tại hoặc đang ẩn.");
            await db.DiemHanhTrinhs.Where(x => x.HanhTrinhId == id).ExecuteDeleteAsync(ct);
        }

        (row.TieuDe, row.MoTa, row.NgayBatDau, row.NgayKetThuc, row.TrangThai, row.AnhBiaPath, row.VideoPath) =
            (input.TieuDe.Trim(), input.MoTa, input.NgayBatDau, input.NgayKetThuc, input.TrangThai, input.AnhBiaPath, input.VideoPath);
        db.Entry(row).Property(x => x.TieuDe).IsModified = true;
        await db.SaveChangesAsync(ct);

        if (input.Diem is not null)
        {
            db.DiemHanhTrinhs.AddRange(input.Diem.Select(x => new DiemHanhTrinh { HanhTrinhId = id,
                NgayDuKien = input.NgayBatDau.AddDays(x.Ngay - 1), ThuTu = x.ThuTu, DiSanId = x.DiSanId,
                TraiNghiemId = x.TraiNghiemId, GioDuKien = x.Gio, GhiChu = x.GhiChu }));
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return Ok(new { row.HanhTrinhId, row.PhienBan });
    }
}
