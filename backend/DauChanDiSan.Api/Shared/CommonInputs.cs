using System.ComponentModel.DataAnnotations;

namespace DauChanDiSan.Api.Shared;

public sealed record ReviewInput([Range(1, 5)] byte SoSao, [StringLength(1000)] string? NoiDung,
    [StringLength(450)] string? AnhPath, [StringLength(450)] string? VideoPath);

public sealed record PublishInput(bool CongKhai);

public sealed record MediaInput([StringLength(450)] string? AnhPath, [StringLength(450)] string? VideoPath,
    [StringLength(250)] string? ChuThich, [Range(1, short.MaxValue)] short ThuTu);

public sealed record ReviewModerationDto(int NguoiDungId, byte SoSao, string? NoiDung, bool CongKhai, DateTime TaoLucUtc);
