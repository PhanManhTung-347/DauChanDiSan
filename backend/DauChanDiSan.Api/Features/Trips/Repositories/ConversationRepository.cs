using System.Text.Json;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Features.Trips.DTOs;
using DauChanDiSan.Api.Helpers;
using DauChanDiSan.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Features.Trips.Repositories;

public sealed class ConversationRepository(DauChanDiSanDbContext db, SqlProcedures sql)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<List<ConversationDto>> ListAsync(int userId, CancellationToken ct) =>
        await Owned(userId).AsNoTracking().OrderByDescending(x => x.HoiThoaiId).Take(100)
            .Select(ConversationDto.Projection).ToListAsync(ct);

    public async Task<long> CreateAsync(int userId, string title, CancellationToken ct)
    {
        var row = new HoiThoaiAi { NguoiDungId = userId, TieuDe = title };
        db.HoiThoaiAis.Add(row);
        await db.SaveChangesAsync(ct);
        return row.HoiThoaiId;
    }

    public async Task<ConversationDetail> DetailAsync(int userId, long id, CancellationToken ct)
    {
        var header = await Owned(userId).AsNoTracking().Where(x => x.HoiThoaiId == id)
            .Select(ConversationDto.Projection).SingleOrDefaultAsync(ct) ?? throw Missing();
        var messages = await Messages(userId, id).OrderByDescending(x => x.TinNhanAiid).Take(100)
            .Select(x => new ConversationMessage(x.TinNhanAiid, x.Vai, x.NoiDung, x.TaoLucUtc, x.DeXuatHanhTrinhJson,
                x.NguonTraLoiAis.OrderBy(s => s.ThuTu).Select(s =>
                    new MessageSource(s.DiSanId, s.TraiNghiemId, s.TuLieuId, s.TieuDeKhiTrich, s.UrlKhiTrich)).ToList()))
            .ToListAsync(ct);
        messages.Reverse();
        return new ConversationDetail(header, messages);
    }

    public async Task ArchiveAsync(int userId, long id, bool archive, CancellationToken ct)
    {
        var row = await Owned(userId).SingleOrDefaultAsync(x => x.HoiThoaiId == id, ct) ?? throw Missing();
        row.DaLuuTru = archive;
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<AssistantHistory>> HistoryAsync(int userId, long id, CancellationToken ct)
    {
        if (!await Owned(userId).AnyAsync(x => x.HoiThoaiId == id && !x.DaLuuTru, ct))
            throw Missing();
        return await Messages(userId, id).OrderByDescending(x => x.TinNhanAiid).Take(8)
            .Select(x => new AssistantHistory(x.TinNhanAiid, x.Vai, x.NoiDung)).ToListAsync(ct);
    }

    public async Task<List<AssistantSource>> SourcesAsync(int? province, int? heritage, string? term, CancellationToken ct)
    {
        var rows = await sql.RunAsync("dbo.usp_LayNguCanhGemini", ct, ("TinhThanhId", province),
            ("DiSanId", heritage), ("TuKhoa", term), ("GioiHan", 12));
        return rows.Select(x => new AssistantSource((string)x["LoaiNguon"]!, (int)x["NguonId"]!,
            (string?)x["TieuDe"], (string?)x["NoiDung"], (string?)x["UrlNguon"])).ToList();
    }

    public async Task<long> SaveTurnAsync(int userId, long id, long? previousId, string question,
        GeminiResult result, CancellationToken ct, string nhaCungCap = "GEMINI")
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var conversation = await sql.QueryAsync("""
            SELECT HoiThoaiId FROM dbo.HoiThoaiAI WITH (UPDLOCK,HOLDLOCK)
            WHERE HoiThoaiId=@Id AND NguoiDungId=@UserId AND DaLuuTru=0;
            """, ct, ("Id", id), ("UserId", userId));
        if (conversation.Count == 0) throw Missing();

        var last = await Messages(userId, id).MaxAsync(x => (long?)x.TinNhanAiid, ct);
        if (last != previousId) throw new ApiFault(409, "Hội thoại vừa thay đổi. Hãy tải lại trước khi gửi.");

        await sql.RunAsync("dbo.usp_LuuTinNhanGemini", ct, ("NguoiDungId", userId), ("HoiThoaiId", id),
            ("Vai", "USER"), ("NoiDung", question), ("TraKetQua", false));
        var reply = result.Reply;
        var plan = reply.Diem.Count == 0 ? null : TripPlanJson.Encode(reply.Diem);
        var saved = (await sql.RunAsync("dbo.usp_LuuTinNhanGemini", ct, ("NguoiDungId", userId), ("HoiThoaiId", id),
            ("Vai", "ASSISTANT"), ("NoiDung", reply.TraLoi.Trim()), ("NhaCungCap", nhaCungCap), ("MaMoHinh", result.Model),
            ("TokenVao", result.InputTokens), ("TokenRa", result.OutputTokens), ("DeXuatHanhTrinhJson", plan),
            ("NguonJson", JsonSerializer.Serialize(reply.Nguon.Distinct(), Json)))).Single();
        await tx.CommitAsync(ct);
        return (long)saved["TinNhanAIId"]!;
    }

    private IQueryable<HoiThoaiAi> Owned(int userId) => db.HoiThoaiAis.Where(x => x.NguoiDungId == userId);
    private IQueryable<TinNhanAi> Messages(int userId, long id) =>
        db.TinNhanAis.AsNoTracking().Where(x => x.HoiThoaiId == id && x.NguoiDungId == userId);
    private static ApiFault Missing() => ApiFault.Missing("Hội thoại không khả dụng.");
}