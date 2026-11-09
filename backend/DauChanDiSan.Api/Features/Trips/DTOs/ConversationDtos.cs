using System.Linq.Expressions;
using DauChanDiSan.Api.Data.Entities;

namespace DauChanDiSan.Api.Features.Trips.DTOs;

public sealed record ConversationDto(long HoiThoaiId, int NguoiDungId, string TieuDe, DateTime TaoLucUtc, bool DaLuuTru)
{
    public static readonly Expression<Func<HoiThoaiAi, ConversationDto>> Projection = x =>
        new(x.HoiThoaiId, x.NguoiDungId, x.TieuDe, x.TaoLucUtc, x.DaLuuTru);
}

public sealed record ConversationDetail(ConversationDto HoiThoai, IReadOnlyList<ConversationMessage> TinNhan);
public sealed record ConversationMessage(long TinNhanAiId, string Vai, string NoiDung, DateTime TaoLucUtc,
    string? DeXuatHanhTrinhJson, IReadOnlyList<MessageSource> Nguon);
public sealed record MessageSource(int? DiSanId, int? TraiNghiemId, int? TuLieuId, string TieuDeKhiTrich, string? UrlKhiTrich);
public sealed record AssistantHistory(long TinNhanAiId, string Vai, string NoiDung);
public sealed record AssistantSource(string Loai, int Id, string? TieuDe, string? NoiDung, string? Url);
public sealed record AssistantReference(string Loai, int Id, string? TieuDe, string? Url);
public sealed record AssistantResponse(long TinNhanAiId, string TraLoi, IReadOnlyList<AssistantReference> Nguon,
    IReadOnlyList<TripPoint> Diem);
