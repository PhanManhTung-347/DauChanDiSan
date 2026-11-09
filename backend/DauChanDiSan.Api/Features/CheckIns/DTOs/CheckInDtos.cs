using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DauChanDiSan.Api.Validation;

namespace DauChanDiSan.Api.Features.CheckIns.DTOs;

public sealed record CheckInInput([Range(1, int.MaxValue)] int DiSanId, [NonEmptyGuid] Guid MaYeuCau,
    [property: JsonRequired][Range(-90, 90)] decimal ViDo, [property: JsonRequired][Range(-180, 180)] decimal KinhDo,
    [Range(typeof(decimal), "0.01", "500")] decimal SaiSoGpsM, [StringLength(500)] string? GhiChu,
    [StringLength(450)] string? AnhPath, [StringLength(450)] string? VideoPath);

public sealed record CheckInDto(long CheckInId, int DiSanId, string TenDiSan, DateOnly? NgayDiaPhuong,
    DateTime ThoiGianUtc, decimal KhoangCachM, string? GhiChu, string? AnhPath, string? VideoPath);

public sealed record CheckInReceipt(long CheckInId, int DiSanId, DateTime ThoiGianUtc, DateOnly? NgayDiaPhuong,
    decimal KhoangCachM, short BanKinhApDungM);
