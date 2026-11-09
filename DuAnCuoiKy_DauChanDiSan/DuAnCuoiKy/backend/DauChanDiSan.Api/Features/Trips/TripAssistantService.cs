using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DauChanDiSan.Api.Features.Trips.DTOs;
using DauChanDiSan.Api.Features.Trips.Repositories;
using DauChanDiSan.Api.Shared;

namespace DauChanDiSan.Api.Features.Trips;

public sealed class TripAssistantService(
    ConversationRepository conversations,
    LocalIntentService localIntent,
    SqlProcedures sql,
    IWebHostEnvironment environment)
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    private static readonly Region[] Regions =
    [
        new("Phố cổ Hội An", "pho-co-hoi-an", null, "hoi an"),
        new("Làng gốm Thanh Hà", "lang-gom-thanh-ha", null, "thanh ha"),
        new("Thánh địa Mỹ Sơn", "thanh-dia-my-son", null, "my son"),
        new("Đại Nội Huế", "dai-noi-hue", null, "hue"),
        new("Làng gốm Bát Tràng", "lang-gom-bat-trang", null, "bat trang"),
        new("Trung tâm Đà Nẵng", null, "DA_NANG", "da nang")
    ];

    public async Task<AssistantResponse> AskAsync(
        int userId, long id, AskInput input, CancellationToken ct)
    {
        var history = await conversations.HistoryAsync(userId, id, ct);
        var question = input.CauHoi.Trim();

        if (question.Length == 0)
            throw ApiFault.Invalid("Bạn hãy nhập câu hỏi.");

        var intent = await localIntent.PredictAsync(question, ct);
        var text = " " + Normalize(question + " " + input.TuKhoa) + " ";
        var region = Regions.FirstOrDefault(x => text.Contains($" {x.Keyword} "));
        var oldQuangNam = region is null && text.Contains(" quang nam ");

        var points = new List<TripPoint>();
        var references = new List<AssistantReference>();
        string answer;

        if (region is null && !oldQuangNam &&
            input.TinhThanhId is null && input.DiSanId is null)
        {
            answer = "Bạn muốn khám phá khu vực nào? Bạn có thể thử Huế, Hội An, "
                + "Thanh Hà, Mỹ Sơn, Đà Nẵng hoặc Bát Tràng. "
                + "Nếu có ngân sách, hãy ghi rõ số tiền cho cả nhóm hoặc mỗi người.";
        }
        else
        {
            var heritage = await LoadHeritageAsync(input, region, oldQuangNam, ct);

            var askAboutServices = text.Contains(" khach san ")
                || text.Contains(" homestay ")
                || text.Contains(" cho o ")
                || text.Contains(" quan an ");

            var asksForTrip = text.Contains(" hanh trinh ")
                || text.Contains(" lich trinh ")
                || text.Contains(" xep lich ")
                || text.Contains(" len lich ");

            if (intent == "TIM_DI_TICH" && !askAboutServices && !asksForTrip)
            {
                answer = heritage.Count == 0
                    ? "Mình chưa tìm thấy di sản công khai phù hợp trong dữ liệu."
                    : "Các di sản công khai trong khu vực:\n"
                      + string.Join("\n", heritage.Select(x => $"- {x.Name} — {x.Summary}"));

                references.AddRange(heritage.Select(x =>
                    new AssistantReference("DI_SAN", x.Id, x.Name, $"/di-san/{x.Slug}")));
            }
            else
            {
                var first = heritage.FirstOrDefault();
                var serviceRegion = region
                    ?? (first is null ? null : Regions.FirstOrDefault(x => x.Slug == first.Slug));

                var provinceId = first?.ProvinceId;
                if (provinceId is null && input.DiSanId is null && region?.Slug is null && !oldQuangNam)
                {
                    var rows = await sql.QueryAsync("""
                        SELECT TinhThanhId
                        FROM dbo.TinhThanh
                        WHERE HoatDong = 1
                          AND (@TinhThanhId IS NOT NULL OR @Ma IS NOT NULL)
                          AND (@TinhThanhId IS NULL OR TinhThanhId = @TinhThanhId)
                          AND (@Ma IS NULL OR Ma = @Ma);
                        """, ct, ("TinhThanhId", input.TinhThanhId), ("Ma", region?.ProvinceCode));

                    if (rows.Count > 0)
                        provinceId = (int)rows[0]["TinhThanhId"]!;
                }

                var services = provinceId is int validProvince
                    ? await LoadServicesAsync(validProvince, serviceRegion?.Area, ct)
                    : new List<TourismService>();

                var area = serviceRegion?.Area ?? first?.ProvinceName ?? "khu vực đã chọn";

                if (askAboutServices && !asksForTrip &&
                    intent is not ("LAP_THEO_NGAN_SACH" or "SUA_HANH_TRINH"))
                {
                    var matching = services.AsEnumerable();

                    if (text.Contains(" khach san "))
                        matching = matching.Where(x => x.Type == "KHACH_SAN");
                    else if (text.Contains(" homestay "))
                        matching = matching.Where(x => x.Type == "HOMESTAY");
                    else if (text.Contains(" quan an "))
                        matching = matching.Where(x => x.Type == "QUAN_AN");
                    else
                        matching = matching.Where(x => x.Type is "KHACH_SAN" or "HOMESTAY");

                    var selected = matching.Take(8).ToList();
                    answer = selected.Count == 0
                        ? $"Mình chưa có dịch vụ phù hợp quanh {area}."
                        : $"Dịch vụ tham khảo quanh {area}:\n"
                          + string.Join("\n", selected.Select(x => "- " + Describe(x)));

                    references.AddRange(selected.Select(ServiceReference));
                }
                else
                {
                    var people = ReadPeople(text);
                    var budget = ReadBudget(text, people);

                    answer = await BuildPlanAsync(
                        input.SoNgay, people, budget, area, heritage, services,
                        points, references, ct);
                }
            }
        }

        var uniqueReferences = references
            .DistinctBy(x => (x.Loai, x.Id))
            .ToList();

        var result = new GeminiResult(
            new GeminiReply(
                answer,
                uniqueReferences.Select(x => new GeminiCitation(x.Loai, x.Id)).ToList(),
                points),
            "intent_tree+sql",
            null,
            null);

        var savedId = await conversations.SaveTurnAsync(
            userId, id, history.FirstOrDefault()?.TinNhanAiId,
            question, result, ct, nhaCungCap: "LOCAL");

        return new AssistantResponse(savedId, answer, uniqueReferences, points);
    }

    private async Task<List<Heritage>> LoadHeritageAsync(
        AskInput input, Region? region, bool oldQuangNam, CancellationToken ct)
    {
        var rows = await sql.QueryAsync("""
            SELECT TOP (14) d.DiSanId, d.TinhThanhId, d.TinhThanh, d.Slug,
                   d.Ten, d.TomTat, d.GiaVeThamKhao
            FROM dbo.vw_DiSanCongKhai d
            JOIN dbo.TinhThanh t ON t.TinhThanhId = d.TinhThanhId
            WHERE (@DiSanId IS NULL OR d.DiSanId = @DiSanId)
              AND (@TinhThanhId IS NULL OR d.TinhThanhId = @TinhThanhId)
              AND (@Slug IS NULL OR d.Slug = @Slug)
              AND (@Ma IS NULL OR t.Ma = @Ma)
              AND (@QuangNam = 0 OR d.Slug IN (
                  'pho-co-hoi-an', 'lang-gom-thanh-ha', 'thanh-dia-my-son'))
              AND (@TrungTam = 0 OR d.Slug NOT IN (
                  'pho-co-hoi-an', 'lang-gom-thanh-ha', 'thanh-dia-my-son'))
            ORDER BY CASE WHEN d.Slug = 'pho-co-hoi-an' THEN 0 ELSE 1 END, d.DiSanId;
            """, ct,
            ("DiSanId", input.DiSanId), ("TinhThanhId", input.TinhThanhId),
            ("Slug", region?.Slug), ("Ma", region?.ProvinceCode),
            ("QuangNam", oldQuangNam), ("TrungTam", region?.ProvinceCode == "DA_NANG"));

        return rows.Select(x => new Heritage(
            (int)x["DiSanId"]!, (int)x["TinhThanhId"]!, (string)x["TinhThanh"]!,
            (string)x["Slug"]!, (string)x["Ten"]!, (string)x["TomTat"]!,
            (decimal?)x["GiaVeThamKhao"])).ToList();
    }

    private async Task<List<TourismService>> LoadServicesAsync(
        int provinceId, string? area, CancellationToken ct)
    {
        var rows = await sql.RunAsync("dbo.usp_LayDichVuDuLich", ct,
            ("TinhThanhId", provinceId),
            ("KhuVuc", area),
            ("GioiHan", 30),
            ("BaoGomDemo", environment.IsDevelopment()));

        return rows.Select(x => new TourismService(
            (int)x["DichVuId"]!, (string)x["Loai"]!, (string)x["Ten"]!,
            (string?)x["KhuVuc"], (decimal?)x["GiaVnd"],
            (short?)x["SucChuaPhong"], (bool)x["LaDuLieuDemo"]!)).ToList();
    }

    private async Task<string> BuildPlanAsync(
        int days, int people, decimal? budget, string area,
        IReadOnlyList<Heritage> heritage, IReadOnlyList<TourismService> services,
        List<TripPoint> points, List<AssistantReference> references, CancellationToken ct)
    {
        if (heritage.Count == 0 && services.Count == 0)
            return $"Mình chưa có đủ dữ liệu công khai để gợi ý cho {area}.";

        var nights = days - 1;
        var lodging = services
            .Where(x => (x.Type is "KHACH_SAN" or "HOMESTAY")
                        && x.Price.HasValue && x.Capacity is > 0)
            .OrderBy(x => x.Price!.Value * RoomsFor(people, x))
            .FirstOrDefault();

        var restaurant = services
            .Where(x => x.Type == "QUAN_AN" && x.Price.HasValue)
            .OrderBy(x => x.Price)
            .FirstOrDefault();

        var answer = new StringBuilder()
            .AppendLine($"Gợi ý khám phá {area} trong {days} ngày cho {people} người.");

        var notes = new List<string>();
        decimal subtotal = 0;

        if (nights > 0 && lodging is not null)
        {
            var rooms = RoomsFor(people, lodging);
            var lodgingCost = lodging.Price!.Value * rooms * nights;
            subtotal += lodgingCost;
            answer.AppendLine($"Chỗ ở — {Describe(lodging)}. {rooms} phòng × {nights} đêm = {Money(lodgingCost)}.");
            notes.Add($"Chỗ ở: {lodging.Name}");
            references.Add(ServiceReference(lodging));
        }
        else if (nights > 0)
        {
            answer.AppendLine("Chỗ ở — chưa có giá phòng để dự toán.");
        }

        if (restaurant is not null)
        {
            var mealsPerPerson = days * 2;
            var foodCost = restaurant.Price!.Value * people * mealsPerPerson;
            subtotal += foodCost;
            answer.AppendLine($"Ăn uống — {Describe(restaurant)}. {people} người × {mealsPerPerson} bữa = {Money(foodCost)}.");
            notes.Add($"Ăn uống: {restaurant.Name}");
            references.Add(ServiceReference(restaurant));
        }
        else
        {
            answer.AppendLine("Ăn uống — chưa có giá quán ăn để dự toán.");
        }

        var tripNotes = notes.Count == 0 ? null : string.Join(" | ", notes);

        for (var day = 1; day <= days; day++)
        {
            if (day > heritage.Count)
            {
                answer.AppendLine($"Ngày {day} — còn trống, chưa có thêm di sản phù hợp trong dữ liệu.");
                continue;
            }

            var place = heritage[day - 1];
            var ticket = place.TicketPrice is decimal price ? Money(price) + "/người" : "chưa có giá vé";
            answer.AppendLine($"Ngày {day}, 08:30 — {place.Name} ({ticket}).");

            if (place.TicketPrice is decimal ticketPrice)
                subtotal += ticketPrice * people;

            points.Add(new TripPoint(day, 1, place.Id, null, new TimeOnly(8, 30),
                day == 1 ? tripNotes : null));

            references.Add(new AssistantReference("DI_SAN", place.Id, place.Name, $"/di-san/{place.Slug}"));

            var experiences = await sql.QueryAsync("""
                SELECT TOP (1) TraiNghiemId, Slug, Ten, GiaMoiNguoi
                FROM dbo.vw_TraiNghiemCongKhai
                WHERE DiSanId = @DiSanId
                ORDER BY CASE WHEN GiaMoiNguoi IS NULL THEN 1 ELSE 0 END,
                         GiaMoiNguoi, TraiNghiemId;
                """, ct, ("DiSanId", place.Id));

            if (experiences.Count == 0)
                continue;

            var activity = experiences[0];
            var activityId = (int)activity["TraiNghiemId"]!;
            var activityName = (string)activity["Ten"]!;
            var activityPrice = (decimal?)activity["GiaMoiNguoi"];

            answer.AppendLine(activityPrice is decimal knownPrice
                ? $"  Trải nghiệm tùy chọn — {activityName}, {Money(knownPrice)}/người."
                : $"  Trải nghiệm tùy chọn — {activityName}, chưa có giá.");

            references.Add(new AssistantReference(
                "TRAI_NGHIEM", activityId, activityName, $"/trai-nghiem/{activity["Slug"]}"));
        }

        answer.AppendLine($"Tạm tính các khoản đã có giá: {Money(subtotal)}.");
        answer.AppendLine("Chưa gồm di chuyển, trải nghiệm tùy chọn và những khoản chưa có giá.");

        if (budget is decimal limit)
        {
            answer.AppendLine(subtotal > limit
                ? $"Các khoản đã có giá vượt ngân sách {Money(subtotal - limit)}."
                : $"Ngân sách cả nhóm {Money(limit)}. Còn {Money(limit - subtotal)} sau các khoản đã có giá.");
        }

        return answer.ToString().TrimEnd();
    }

    private static int ReadPeople(string text)
    {
        var match = Regex.Match(text,
            @"\b(?<n>\d{1,3}|mot|hai|ba|bon|nam|sau|bay|tam|chin|muoi)\s+nguoi\b");

        if (!match.Success)
            return 1;

        var value = match.Groups["n"].Value;

        if (int.TryParse(value, out var number))
            return Math.Clamp(number, 1, 100);

        return value switch
        {
            "hai" => 2,
            "ba" => 3,
            "bon" => 4,
            "nam" => 5,
            "sau" => 6,
            "bay" => 7,
            "tam" => 8,
            "chin" => 9,
            "muoi" => 10,
            _ => 1
        };
    }

    private static decimal? ReadBudget(string text, int people)
    {
        var match = Regex.Match(text,
            @"\b(?<n>\d{1,9}(?:[.,]\d{1,2})?)\s*(?<unit>trieu|nghin|ngan|k)\b");

        decimal amount;
        if (match.Success && decimal.TryParse(
            match.Groups["n"].Value.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
        {
            amount = number * (match.Groups["unit"].Value == "trieu" ? 1_000_000m : 1_000m);
        }
        else
        {
            match = Regex.Match(text,
                @"\b(?<n>\d{1,3}(?:[.,]\d{3}){1,3}|\d{4,12})\s*(?:vnd|dong|d)\b");

            if (!match.Success || !decimal.TryParse(
                match.Groups["n"].Value.Replace(".", "").Replace(",", ""),
                NumberStyles.None, CultureInfo.InvariantCulture, out amount))
                return null;
        }

        return text.Contains(" moi nguoi ") ? amount * people : amount;
    }

    private static string Normalize(string value)
    {
        var buffer = new StringBuilder();
        foreach (var ch in value.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                buffer.Append(ch == 'đ' ? 'd' : ch);
        }

        return Regex.Replace(buffer.ToString(), @"[^a-z0-9.,]+", " ")
            .Replace(", ", " ")
            .Replace(". ", " ")
            .TrimEnd('.', ',');
    }

    private static int RoomsFor(int people, TourismService service) =>
        (int)Math.Ceiling((decimal)people / service.Capacity!.Value);

    private static string Money(decimal amount) => amount.ToString("N0", Vi) + " đ";

    private static string Describe(TourismService service)
    {
        var name = service.IsDemo ? "[DEMO] " + service.Name : service.Name;
        var unit = service.Type == "QUAN_AN" ? "/người/bữa" : "/phòng/đêm";
        var price = service.Price is decimal value ? Money(value) + unit : "chưa có giá";
        return $"{name} — {service.Area}, {price}";
    }

    private static AssistantReference ServiceReference(TourismService service) =>
        new("DICH_VU", service.Id, service.IsDemo ? "[DEMO] " + service.Name : service.Name, null);

    private sealed record Region(string Area, string? Slug, string? ProvinceCode, string Keyword);
    private sealed record Heritage(int Id, int ProvinceId, string ProvinceName, string Slug,
        string Name, string Summary, decimal? TicketPrice);
    private sealed record TourismService(int Id, string Type, string Name, string? Area,
        decimal? Price, short? Capacity, bool IsDemo);
}