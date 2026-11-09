using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Discovery;

public sealed record QuizAnswer(int CauHoiId, int LuaChonId);
public sealed record QuizAttempt([Required, MinLength(1), MaxLength(100)] QuizAnswer[] TraLoi);

[ApiController, Route("api/di-san/{id:int}/bai-kiem-tra")]
public sealed class HeritageLearningController(DauChanDiSanDbContext db, SqlProcedures sql, CurrentUser user) : ControllerBase
{
    private const int Points = 30;

    [HttpGet]
    public async Task<IActionResult> Read(int id, CancellationToken ct)
    {
        var quiz = await FindQuiz(id, ct);
        var questions = await db.CauHoiNhiemVus.AsNoTracking().Where(x => x.NhiemVuId == quiz)
            .OrderBy(x => x.ThuTu).Select(x => new { x.CauHoiId, x.NoiDung,
                luaChon = x.LuaChonCauHois.OrderBy(a => a.ThuTu).Select(a => new { a.LuaChonId, a.NoiDung }) }).ToListAsync(ct);
        var earned = user.OptionalId is int account && (await sql.QueryAsync(
            "SELECT Diem FROM dbo.DiemDiSan WHERE NguoiDungId=@User AND DiSanId=@Id;", ct, ("User", account), ("Id", id))).Count > 0;
        return Ok(new { cauHoi = questions, diemThuong = Points, daNhanDiem = earned });
    }

    [HttpPost, Authorize(Policy = Roles.Account)]
    public async Task<IActionResult> Submit(int id, QuizAttempt input, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var quiz = await FindQuiz(id, ct) ?? throw ApiFault.Missing("Di sản chưa mở bài kiểm tra.");
        var mission = await db.NhiemVus.FromSqlInterpolated(
            $"SELECT * FROM dbo.NhiemVu WITH (UPDLOCK,HOLDLOCK) WHERE NhiemVuId={quiz}").SingleAsync(ct);
        if (!mission.CongKhai || mission.KetThucUtc <= DateTime.UtcNow || mission.BatDauUtc > DateTime.UtcNow)
            throw ApiFault.Missing("Bài kiểm tra chưa mở.");
        var questions = await db.CauHoiNhiemVus.AsNoTracking().Include(x => x.LuaChonCauHois)
            .Where(x => x.NhiemVuId == quiz).OrderBy(x => x.ThuTu).ToListAsync(ct);
        if (questions.Count == 0 || input.TraLoi.Length != questions.Count || input.TraLoi.Any(x => x is null)
            || input.TraLoi.Select(x => x.CauHoiId).Distinct().Count() != questions.Count)
            throw ApiFault.Invalid("Hãy chọn một đáp án cho mỗi câu hỏi.");
        var selected = input.TraLoi.ToDictionary(x => x.CauHoiId, x => x.LuaChonId);
        var detail = questions.Select(q =>
        {
            var answer = selected.TryGetValue(q.CauHoiId, out var key) ? q.LuaChonCauHois.SingleOrDefault(x => x.LuaChonId == key) : null;
            return new { q.CauHoiId, Dung = (answer ?? throw ApiFault.Invalid("Đáp án không hợp lệ.")).Dung, q.GiaiThich };
        }).ToArray();
        var correct = detail.Count(x => x.Dung);
        var pass = correct * 3 >= questions.Count * 2;
        var points = 0;
        if (pass)
        {
            var result = await sql.QueryAsync("""
                INSERT dbo.DiemDiSan(NguoiDungId,DiSanId,Diem)
                SELECT @User,@Id,@Points WHERE NOT EXISTS(
                    SELECT 1 FROM dbo.DiemDiSan WITH (UPDLOCK,HOLDLOCK) WHERE NguoiDungId=@User AND DiSanId=@Id);
                SELECT @@ROWCOUNT AS Added;
                """, ct, ("User", user.Id), ("Id", id), ("Points", Points));
            if (result.Single()["Added"] is 1) points = Points;
        }
        await transaction.CommitAsync(ct);
        return Ok(new { soDung = correct, tong = questions.Count, dat = pass, diemCong = points, chiTiet = detail });
    }

    private async Task<int?> FindQuiz(int id, CancellationToken ct)
    {
        var rows = await sql.QueryAsync("""
            SELECT n.NhiemVuId FROM dbo.BaiKiemTraDiSan b
            JOIN dbo.NhiemVu n ON n.NhiemVuId=b.NhiemVuId AND n.DiSanId=b.DiSanId
            JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId=b.DiSanId
            WHERE b.DiSanId=@Id AND n.CongKhai=1 AND n.BatDauUtc<=SYSUTCDATETIME()
              AND (n.KetThucUtc IS NULL OR n.KetThucUtc>SYSUTCDATETIME());
            """, ct, ("Id", id));
        return rows.SingleOrDefault()?["NhiemVuId"] as int?;
    }
}
