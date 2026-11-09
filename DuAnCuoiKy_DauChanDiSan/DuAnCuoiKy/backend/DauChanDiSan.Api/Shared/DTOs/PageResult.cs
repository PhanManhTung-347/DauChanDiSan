namespace DauChanDiSan.Api.Shared.DTOs;

public sealed record PageResult<T>(int Tong, int Trang, int KichThuoc, IReadOnlyList<T> Muc);
