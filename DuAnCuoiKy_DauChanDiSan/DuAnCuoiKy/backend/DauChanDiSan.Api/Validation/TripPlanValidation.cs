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
            && ((x.DiSanId > 0 && x.TraiNghiemId is null) || (x.TraiNghiemId > 0 && x.DiSanId is null))
            && (x.GhiChu is null || x.GhiChu.Length <= 500))
        && points.Select(x => (x.Ngay, x.ThuTu)).Distinct().Count() == points.Count;
}
