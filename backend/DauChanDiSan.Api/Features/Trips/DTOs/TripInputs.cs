using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Validation;

namespace DauChanDiSan.Api.Features.Trips.DTOs;

public sealed record TripPoint([Range(1, 90)] int Ngay, [Range(1, 100)] short ThuTu,
    int? DiSanId, int? TraiNghiemId, TimeOnly? Gio, [StringLength(500)] string? GhiChu,
    int? DichVuId = null, decimal? DonGiaVnd = null, int? SoLuongTinhGia = null,
    [StringLength(16)] string? DonViTinhGia = null);
public sealed record TripCreateInput([Required, StringLength(160)] string TieuDe,
    DateOnly NgayBatDau, DateOnly NgayKetThuc, [StringLength(1000)] string? MoTa,
    long? TinNhanAiId, [MaxLength(300)] List<TripPoint>? Diem);
public sealed record TripEditInput([Required, StringLength(160)] string TieuDe,
    DateOnly NgayBatDau, DateOnly NgayKetThuc, [StringLength(1000)] string? MoTa, [Required, OneOf("LEN_KE_HOACH", "DANG_DI", "HOAN_THANH", "DA_HUY")] string TrangThai,
    [StringLength(450)] string? AnhBiaPath, [StringLength(450)] string? VideoPath,
    [Required, MinLength(8), MaxLength(8)] byte[] PhienBan, [MaxLength(300)] List<TripPoint>? Diem);
public sealed record ConversationInput([Required, StringLength(160)] string TieuDe);
public sealed record ConversationArchive(bool DaLuuTru);
public sealed record AskInput([Required, StringLength(2500)] string CauHoi, int? TinhThanhId, int? DiSanId,
    [StringLength(100)] string? TuKhoa, [Range(1, 14)] int SoNgay = 1);
public sealed record GeminiCitation(string Loai, int Id);
public sealed record GeminiReply(string TraLoi, List<GeminiCitation> Nguon, List<TripPoint> Diem);
public sealed record GeminiResult(GeminiReply Reply, string Model, int? InputTokens, int? OutputTokens);