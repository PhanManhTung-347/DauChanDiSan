using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Validation;

namespace DauChanDiSan.Api.Features.Discovery.DTOs;

public sealed record ContributionInput(
    [NonEmptyGuid] Guid MaYeuCau,
    [Required, OneOf("DI_SAN_MOI", "BO_SUNG")] string Loai,
    [Required, StringLength(200)] string TieuDe,
    [Required, StringLength(1600)] string NoiDung,
    [Range(1, int.MaxValue)] int? DiSanId,
    [Range(1, int.MaxValue)] int? TinhThanhId,
    [Range(1, int.MaxValue)] int? LoaiDiSanId,
    [StringLength(250)] string? DiaChi,
    [Range(-90, 90)] decimal? ViDo,
    [Range(-180, 180)] decimal? KinhDo,
    [StringLength(450)] string? AnhPath,
    [StringLength(1000), HttpUrl] string? UrlNguon) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Loai == "DI_SAN_MOI"
            && (DiSanId is not null || TinhThanhId is null || LoaiDiSanId is null
                || string.IsNullOrWhiteSpace(DiaChi) || ViDo is null || KinhDo is null))
            yield return new ValidationResult("Đề xuất mới cần tỉnh thành, loại di sản, địa chỉ và tọa độ.");

        if (Loai == "BO_SUNG"
            && (DiSanId is null || TinhThanhId is not null || LoaiDiSanId is not null
                || DiaChi is not null || ViDo is not null || KinhDo is not null))
            yield return new ValidationResult("Bổ sung phải chọn di sản có sẵn. Vị trí lấy từ di sản đó.");
    }
}

public sealed record ReviewContributionInput(
    [Required, OneOf("DA_DUYET", "TU_CHOI")] string QuyetDinh,
    [StringLength(160), RegularExpression("[a-z0-9]+(?:-[a-z0-9]+)*")] string? Slug,
    [StringLength(500)] string? LyDo) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (QuyetDinh == "TU_CHOI" && (string.IsNullOrWhiteSpace(LyDo) || Slug is not null))
            yield return new ValidationResult("Từ chối cần lý do và bỏ trống slug.", [nameof(LyDo), nameof(Slug)]);
        if (QuyetDinh == "DA_DUYET" && LyDo is not null)
            yield return new ValidationResult("Khi duyệt, bỏ trống lý do từ chối.", [nameof(LyDo)]);
    }
}

public sealed record ContributionDto(
    long DongGopId, int NguoiDungId, string Loai, string TieuDe, string NoiDung,
    int? DiSanId, int? TinhThanhId, int? LoaiDiSanId, string? DiaChi, decimal? ViDo, decimal? KinhDo,
    string? AnhPath, string? UrlNguon, string TrangThai, string? LyDo, int? NguoiDuyetId,
    DateTime? DuyetLucUtc, int? DiSanCongKhaiId, int? TuLieuId, DateTime TaoLucUtc, DateTime CapNhatLucUtc)
{
    public static readonly Expression<Func<DongGopDiSan, ContributionDto>> Projection = row =>
        new(row.DongGopId, row.NguoiDungId, row.Loai, row.TieuDe, row.NoiDung,
            row.DiSanId, row.TinhThanhId, row.LoaiDiSanId, row.DiaChi, row.ViDo, row.KinhDo,
            row.AnhPath, row.UrlNguon, row.TrangThai, row.LyDo, row.NguoiDuyetId, row.DuyetLucUtc,
            row.DiSanCongKhaiId, row.TuLieuId, row.TaoLucUtc, row.CapNhatLucUtc);
}
