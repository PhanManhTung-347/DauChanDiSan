using System.Text.Json;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Discovery;

public sealed class RecommendationService(DauChanDiSanDbContext db, SqlProcedures sql)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<object> SuggestAsync(int? userId, int? province, int limit, string placement, CancellationToken ct)
    {
        if (userId is not int id || !await db.NguoiDungs.AnyAsync(x => x.NguoiDungId == id && x.ChoPhepCaNhanHoa, ct))
            return await Baseline(null, province, limit, ct);

        var preferences = await db.SoThichNguoiDungs.Where(x => x.NguoiDungId == id)
            .Select(x => new { x.ChuDe.Ten, x.MucDo }).ToListAsync(ct);
        var history = await db.VwDuLieuHuanLuyens.Where(x => x.NguoiDungId == id && x.DiSanId != null && x.Nhan != null)
            .OrderByDescending(x => x.ThoiGianUtc).Take(100).Select(x => new { Id = x.DiSanId!.Value, x.Nhan })
            .ToListAsync(ct);
        var newest = history.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First().Nhan);

        var rows = await db.VwDiSanCongKhais.AsNoTracking().OrderBy(x => x.DiSanId).Take(1500).ToListAsync(ct);
        var ids = rows.Select(x => x.DiSanId).ToArray();
        var topics = await db.DiSans.Where(x => ids.Contains(x.DiSanId))
            .Select(x => new { x.DiSanId, Tags = x.ChuDes.Select(c => c.Ten) }).ToListAsync(ct);
        var labels = topics.ToDictionary(x => x.DiSanId, x => string.Join(" ", x.Tags));
        var corpus = rows.ToDictionary(x => x.DiSanId,
            x => string.Join(" ", x.Ten, x.LoaiDiSan, x.TomTat, labels.GetValueOrDefault(x.DiSanId)));
        var text = string.Join(" ", preferences.SelectMany(x => Enumerable.Repeat(x.Ten, x.MucDo))
            .Concat(newest.Where(x => x.Value == 1).Take(15).Select(x => corpus.GetValueOrDefault(x.Key) ?? "")));
        if (string.IsNullOrWhiteSpace(text))
            return await Baseline(null, province, limit, ct);

        var scores = TfIdfModel.Fit(corpus).Score(text);
        var ranked = rows.Where(x => (province is null || x.TinhThanhId == province) && newest.GetValueOrDefault(x.DiSanId) != 0)
            .Select(x => new { Item = x, Score = Math.Round(scores[x.DiSanId], 6) })
            .Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenBy(x => x.Item.DiSanId).Take(limit).ToList();
        if (ranked.Count == 0)
            return await Baseline(null, province, limit, ct);

        var plan = JsonSerializer.Serialize(ranked.Select(x => new
        {
            x.Item.DiSanId, diem = x.Score, lyDo = "Phù hợp chủ đề bạn quan tâm."
        }), Json);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var modelRow = (await sql.QueryAsync("""
            SET NOCOUNT ON;
            IF NOT EXISTS (SELECT 1 FROM dbo.MoHinhGoiY WITH (UPDLOCK,HOLDLOCK)
                           WHERE Ma='CONTENT_TFIDF' AND PhienBan='1.0')
                INSERT dbo.MoHinhGoiY(Ma,PhienBan,ThuatToan)
                    VALUES ('CONTENT_TFIDF','1.0',N'TF-IDF + cosine');
            SELECT MoHinhId FROM dbo.MoHinhGoiY WHERE Ma='CONTENT_TFIDF' AND PhienBan='1.0';
            """, ct)).Single();
        var saved = await sql.RunAsync("dbo.usp_LuuLuotGoiY", ct, ("NguoiDungId", id),
            ("MoHinhId", modelRow["MoHinhId"]), ("ViTriHienThi", placement), ("DanhSachJson", plan));
        await tx.CommitAsync(ct);
        var key = saved[0]["LuotGoiYId"];
        return new { phuongPhap = "TF_IDF", luotGoiYId = key, muc = ranked.Select((x, i) =>
            new { x.Item.DiSanId, x.Item.Ten, x.Item.Slug, x.Item.AnhBiaPath, thuHang = i + 1,
                diem = x.Score, lyDo = "Phù hợp chủ đề bạn quan tâm." }) };
    }

    private async Task<object> Baseline(int? userId, int? province, int limit, CancellationToken ct) =>
        new { phuongPhap = "PHO_BIEN", luotGoiYId = (long?)null, muc = await sql.RunAsync("dbo.usp_GoiYTheoSoThich", ct,
            ("NguoiDungId", userId), ("TinhThanhId", province), ("GioiHan", limit)) };
}
