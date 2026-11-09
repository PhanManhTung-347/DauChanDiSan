using DauChanDiSan.Api.Features.Trips.DTOs;
using DauChanDiSan.Api.Shared;

namespace DauChanDiSan.Api.Validation;

public static class TripPlanValidation
{
    public static void RequireValid(DateOnly start, DateOnly end, IReadOnlyList<TripPoint>? points)
    {
        var days = start == default || end == default ? 0 : end.DayNumber - start.DayNumber + 1;
        if (days is < 1 or > 90 || (points is not null && !IsValid(points, days)))
            throw ApiFault.Invalid("Kiểm tra ngày đi, thứ tự và định danh điểm đến.");
    }

    public static bool IsValid(IReadOnlyList<TripPoint> points, int days) =>
        points.Count <= 300
        && points.All(x => x is not null && x.Ngay >= 1 && x.Ngay <= days && x.ThuTu is >= 1 and <= 100
            && (x.DiSanId is null or > 0) && (x.TraiNghiemId is null or > 0) && (x.DichVuId is null or > 0)
            && (x.DiSanId.HasValue ? 1 : 0) + (x.TraiNghiemId.HasValue ? 1 : 0)
                + (x.DichVuId.HasValue ? 1 : 0) == 1
            && HasValidPrice(x)
            && (x.GhiChu is null || x.GhiChu.Length <= 500))
        && points.Select(x => (x.Ngay, x.ThuTu)).Distinct().Count() == points.Count;
    private static bool HasValidPrice(TripPoint point)
    {
        if (point.DonGiaVnd is null && point.SoLuongTinhGia is null && point.DonViTinhGia is null)
            return true;

        return point.DonGiaVnd is >= 0 and <= 99999999999999m
            && decimal.Truncate(point.DonGiaVnd.Value) == point.DonGiaVnd.Value
            && point.SoLuongTinhGia is >= 1 and <= 10000
            && point.DonViTinhGia is "NGUOI_LUOT" or "PHONG_DEM" or "NGUOI_BUA"
            && (point.DichVuId is not null || point.DonViTinhGia == "NGUOI_LUOT");
    }
}