using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Validation;

namespace DauChanDiSan.Api.Features.Experiences.DTOs;

public sealed record BookingInput([Range(1, int.MaxValue)] int LichTraiNghiemId,
    [Range(1, 500)] short SoNguoi, [NonEmptyGuid] Guid MaYeuCau, [StringLength(500)] string? GhiChu);
public sealed record BookingStatusInput([Required, OneOf("DA_XAC_NHAN", "TU_CHOI", "DA_HUY", "DA_THAM_GIA")] string TrangThaiMoi);
public sealed record PartnerInput([Range(1, int.MaxValue)] int TinhThanhId,
    [Required, StringLength(160)] string Ten, [StringLength(1000)] string? MoTa,
    [StringLength(24)] string? DienThoai, [EmailAddress, StringLength(254)] string? Email,
    [StringLength(250)] string? DiaChi, [StringLength(450)] string? AnhDaiDienPath, bool HoatDong = true);
public sealed record PartnerMemberInput([Required, OneOf("QUAN_LY", "NHAN_VIEN")] string Quyen, bool HoatDong);
public sealed record ExperienceInput([Range(1, int.MaxValue)] int DiSanId,
    [Required, StringLength(160), RegularExpression("[a-z0-9]+(?:-[a-z0-9]+)*")] string Slug,
    [Required, StringLength(180)] string Ten, [Required, StringLength(15000)] string MoTa,
    [StringLength(250)] string? DiaDiemTapTrung, [Range(1, 1440)] short ThoiLuongPhut,
    [Range(typeof(decimal), "0", "999999999999")] decimal? GiaMoiNguoi,
    [StringLength(450)] string? AnhBiaPath, [StringLength(450)] string? VideoPath,
    bool GuiDuyet);
public sealed record SlotInput(DateTimeOffset BatDauUtc, DateTimeOffset KetThucUtc,
    [Range(1, 500)] short SucChua, [Required, OneOf("MO", "DONG", "HUY")] string TrangThai);

public sealed record BookingDto(long YeuCauId, int TraiNghiemId, string TenTraiNghiem,
    int LichTraiNghiemId, DateTime BatDauUtc, short SoNguoi, decimal? DonGiaDuKien, string TrangThai,
    string? GhiChu, DateTime TaoLucUtc);
