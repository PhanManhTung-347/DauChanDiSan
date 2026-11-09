using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Validation;

namespace DauChanDiSan.Api.Features.Accounts.DTOs;

public sealed record MissionInput([Required, OneOf("QUIZ", "DEM_DI_SAN")] string Loai, [Required, StringLength(160)] string Ten,
    [Required, StringLength(800)] string MoTa, int? HuyHieuId, int? DiSanId, int? LoaiDiSanId,
    [Range(1, 500)] short? SoDiSanCanDat, DateTimeOffset BatDauUtc, DateTimeOffset? KetThucUtc) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BatDauUtc == default || KetThucUtc <= BatDauUtc)
            yield return new ValidationResult("Thời gian nhiệm vụ không hợp lệ.", [nameof(BatDauUtc), nameof(KetThucUtc)]);

        if ((Loai == "QUIZ" && (SoDiSanCanDat is not null || LoaiDiSanId is not null))
            || (Loai == "DEM_DI_SAN" && (SoDiSanCanDat is null || DiSanId is not null)))
            yield return new ValidationResult("Mục tiêu không phù hợp với loại nhiệm vụ.");
    }
}
public sealed record QuestionInput([Range(1, short.MaxValue)] short ThuTu,
    [Required, StringLength(500)] string NoiDung, [StringLength(800)] string? GiaiThich,
    [Required, MinLength(2), MaxLength(10)] List<AnswerInput> LuaChon) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LuaChon is not null && (LuaChon.Any(x => x is null) || LuaChon.Count(x => x.Dung) != 1))
            yield return new ValidationResult("Câu hỏi phải có đúng một đáp án đúng.", [nameof(LuaChon)]);
    }
}
public sealed record AnswerInput([Required, StringLength(400)] string NoiDung, bool Dung);
public sealed record QuizSubmission([Range(1, int.MaxValue)] int LuaChonId);
public sealed record BadgeInput([Required, StringLength(40)] string Ma,
    [Required, StringLength(120)] string Ten, [StringLength(500)] string? MoTa, [StringLength(450)] string? AnhPath);
