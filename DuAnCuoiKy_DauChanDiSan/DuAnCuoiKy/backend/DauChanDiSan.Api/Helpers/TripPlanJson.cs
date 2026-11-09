using System.Text.Json;
using DauChanDiSan.Api.Features.Trips.DTOs;

namespace DauChanDiSan.Api.Helpers;

public static class TripPlanJson
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Encode(IReadOnlyList<TripPoint> points) =>
        JsonSerializer.Serialize(new { diem = points }, Json);
}
