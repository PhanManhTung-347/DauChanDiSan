using DauChanDiSan.Api.Features.Accounts.DTOs;
using DauChanDiSan.Api.Validation;
using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Accounts;

[ApiController, Route("api/nhiem-vu"), Authorize(Policy = Roles.Account)]
public sealed class MissionsController(DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var missions = await db.NhiemVus.AsNoTracking().Where(x => x.CongKhai && x.BatDauUtc <= now
                && (x.KetThucUtc == null || x.KetThucUtc > now)).OrderBy(x => x.NhiemVuId).ToListAsync(ct);
        var links = (await sql.QueryAsync("""
            SELECT b.NhiemVuId,d.Slug FROM dbo.BaiKiemTraDiSan b LEFT JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId=b.DiSanId;
            """, ct)).ToDictionary(row => (int)row["NhiemVuId"]!, row => row["Slug"] as string);
        return Ok(missions.Where(x => !links.ContainsKey(x.NhiemVuId) || links[x.NhiemVuId] is not null)
            .Select(x => new { x.NhiemVuId, x.HuyHieuId, x.DiSanId, x.LoaiDiSanId, x.Loai, x.Ten, x.MoTa,
                x.SoDiSanCanDat, x.BatDauUtc, x.KetThucUtc, x.CongKhai, slugBaiDoc = links.GetValueOrDefault(x.NhiemVuId) }));
    }

    [HttpGet("{id:int}/cau-hoi"), AllowAnonymous]
    public async Task<IActionResult> Questions(int id, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (!await db.NhiemVus.AnyAsync(x => x.NhiemVuId == id && x.CongKhai && x.Loai == "QUIZ"
                && x.BatDauUtc <= now && (x.KetThucUtc == null || x.KetThucUtc > now), ct))
            throw ApiFault.Missing("Quiz chưa mở.");
        return Ok(await db.CauHoiNhiemVus.AsNoTracking().Where(x => x.NhiemVuId == id).OrderBy(x => x.ThuTu)
            .Select(x => new { x.CauHoiId, x.ThuTu, x.NoiDung,
                luaChon = x.LuaChonCauHois.OrderBy(y => y.ThuTu).Select(y => new { y.LuaChonId, y.ThuTu, y.NoiDung }) })
            .ToListAsync(ct));
    }

    [HttpPost("cau-hoi/{id:int}/tra-loi")]
    public async Task<IActionResult> Submit(int id, QuizSubmission input, CancellationToken ct) =>
        Ok((await sql.RunAsync("dbo.usp_NopCauTraLoi", ct, ("NguoiDungId", user.Id),
            ("CauHoiId", id), ("LuaChonId", input.LuaChonId))).Single());

    [HttpPost("{id:int}/tien-do")]
    public async Task<IActionResult> Progress(int id, CancellationToken ct) =>
        Ok((await sql.RunAsync("dbo.usp_CapNhatTienDoNhiemVu", ct, ("NguoiDungId", user.Id),
            ("NhiemVuId", id))).Single());

    [HttpGet("huy-hieu/toi")]
    public async Task<IActionResult> Badges(CancellationToken ct) =>
        Ok(await (from earned in db.HuyHieuNguoiDungs.AsNoTracking()
                  join badge in db.HuyHieus on earned.HuyHieuId equals badge.HuyHieuId
                  where earned.NguoiDungId == user.Id
                  orderby earned.NhanLucUtc descending
                  select new { badge.HuyHieuId, badge.Ma, badge.Ten, badge.MoTa, badge.AnhPath, earned.NhanLucUtc })
            .ToListAsync(ct));
}

[ApiController, Route("api/quan-tri/nhiem-vu"), Authorize(Roles = Roles.Admin)]
public sealed class MissionAdminController(DauChanDiSanDbContext db, CurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.NhiemVus.AsNoTracking().OrderByDescending(x => x.NhiemVuId).Take(100).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(MissionInput input, CancellationToken ct)
    {
        var row = new NhiemVu();
        ApplyMission(row, input);
        db.NhiemVus.Add(row);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { row.NhiemVuId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, MissionInput input, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var row = await LockMissionAsync(id, ct);
        if (row.CongKhai || await db.TienDoNhiemVus.AnyAsync(x => x.NhiemVuId == id, ct))
            throw new ApiFault(409, "Chỉ sửa nhiệm vụ đang ẩn và chưa có tiến độ.");
        if (input.Loai != row.Loai && await db.CauHoiNhiemVus.AnyAsync(x => x.NhiemVuId == id, ct))
            throw new ApiFault(409, "Hãy xóa câu hỏi nháp trước khi đổi loại nhiệm vụ.");
        ApplyMission(row, input);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return NoContent();
    }

    [HttpGet("huy-hieu")]
    public async Task<IActionResult> ListBadges(CancellationToken ct) =>
        Ok(await db.HuyHieus.AsNoTracking().OrderBy(x => x.Ten).ToListAsync(ct));

    [HttpPost("huy-hieu")]
    public async Task<IActionResult> CreateBadge(BadgeInput input, CancellationToken ct)
    {
        MediaValidation.RequireOwner(user.Id, input.AnhPath);
        var row = new HuyHieu { Ma = input.Ma.Trim(), Ten = input.Ten.Trim(), MoTa = input.MoTa, AnhPath = input.AnhPath };
        db.HuyHieus.Add(row);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { row.HuyHieuId });
    }

    [HttpGet("{id:int}/cau-hoi")]
    public async Task<IActionResult> ListQuestions(int id, CancellationToken ct) =>
        Ok(await db.CauHoiNhiemVus.AsNoTracking().Where(x => x.NhiemVuId == id)
            .OrderBy(x => x.ThuTu).Select(x => new { x.CauHoiId, x.ThuTu, x.NoiDung, x.GiaiThich,
                luaChon = x.LuaChonCauHois.OrderBy(y => y.ThuTu).Select(y => new { y.LuaChonId, y.ThuTu, y.NoiDung, y.Dung }) })
            .ToListAsync(ct));

    [HttpPost("{id:int}/cau-hoi")]
    [HttpPut("{id:int}/cau-hoi/{questionId:int}")]
    public async Task<IActionResult> SaveQuestion(int id, int? questionId, QuestionInput input, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireEditableQuizAsync(id, ct);
        var question = questionId is int key
            ? await db.CauHoiNhiemVus.SingleOrDefaultAsync(x => x.NhiemVuId == id && x.CauHoiId == key, ct)
                ?? throw ApiFault.Missing("Không tìm thấy câu hỏi.")
            : new CauHoiNhiemVu { NhiemVuId = id };
        (question.ThuTu, question.NoiDung, question.GiaiThich) =
            (input.ThuTu, input.NoiDung.Trim(), input.GiaiThich);
        if (questionId is null) db.CauHoiNhiemVus.Add(question);
        else await db.LuaChonCauHois.Where(x => x.CauHoiId == questionId).ExecuteDeleteAsync(ct);

        for (var i = 0; i < input.LuaChon.Count; i++)
        {
            var option = input.LuaChon[i];
            question.LuaChonCauHois.Add(new LuaChonCauHoi
            {
                ThuTu = (byte)(i + 1), NoiDung = option.NoiDung.Trim(), Dung = option.Dung
            });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return StatusCode(questionId is null ? 201 : 200, new { question.CauHoiId });
    }

    [HttpDelete("{id:int}/cau-hoi/{questionId:int}")]
    public async Task<IActionResult> DeleteQuestion(int id, int questionId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireEditableQuizAsync(id, ct);
        var question = await db.CauHoiNhiemVus.SingleOrDefaultAsync(x => x.NhiemVuId == id && x.CauHoiId == questionId, ct)
            ?? throw ApiFault.Missing("Không tìm thấy câu hỏi.");
        await db.LuaChonCauHois.Where(x => x.CauHoiId == questionId).ExecuteDeleteAsync(ct);
        db.CauHoiNhiemVus.Remove(question);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return NoContent();
    }

    [HttpPut("{id:int}/cong-khai")]
    public async Task<IActionResult> Publish(int id, PublishInput input, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var mission = await LockMissionAsync(id, ct);
        if (input.CongKhai && mission.Loai == "QUIZ" && !await db.CauHoiNhiemVus.AnyAsync(x => x.NhiemVuId == id, ct))
            throw ApiFault.Invalid("Quiz cần ít nhất một câu hỏi.");
        mission.CongKhai = input.CongKhai;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return NoContent();
    }

    private async Task<NhiemVu> LockMissionAsync(int id, CancellationToken ct) =>
        await db.NhiemVus.FromSqlInterpolated($"SELECT * FROM dbo.NhiemVu WITH (UPDLOCK,HOLDLOCK) WHERE NhiemVuId={id}")
            .SingleOrDefaultAsync(ct) ?? throw ApiFault.Missing("Nhiệm vụ không tồn tại.");

    private async Task RequireEditableQuizAsync(int id, CancellationToken ct)
    {
        var mission = await LockMissionAsync(id, ct);
        if (mission.CongKhai || mission.Loai != "QUIZ")
            throw ApiFault.Invalid("Chỉ chỉnh sửa câu hỏi của quiz đang ẩn.");
        if (await db.KetQuaTraLois.AnyAsync(x => x.LuaChonCauHoi.CauHoi.NhiemVuId == id, ct))
            throw new ApiFault(409, "Quiz đã có lượt trả lời. Hãy tạo nhiệm vụ mới.");
    }

    private static void ApplyMission(NhiemVu row, MissionInput input)
    {
        (row.Loai, row.Ten, row.MoTa, row.HuyHieuId, row.DiSanId, row.LoaiDiSanId, row.SoDiSanCanDat) =
            (input.Loai, input.Ten.Trim(), input.MoTa.Trim(), input.HuyHieuId, input.DiSanId, input.LoaiDiSanId, input.SoDiSanCanDat);
        row.BatDauUtc = input.BatDauUtc.UtcDateTime;
        row.KetThucUtc = input.KetThucUtc?.UtcDateTime;
    }
}
