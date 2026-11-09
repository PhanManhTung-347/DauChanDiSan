using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DauChanDiSan.Api.Features.Trips.DTOs;
using DauChanDiSan.Api.Features.Trips.Repositories;
using DauChanDiSan.Api.Shared;
using DauChanDiSan.Api.Validation;

namespace DauChanDiSan.Api.Features.Trips;

public sealed class TripAssistantService(
    ConversationRepository conversations, LocalIntentService localIntent,
    SqlProcedures sql, IWebHostEnvironment environment)
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public async Task<AssistantResponse> AskAsync(int userId, long id, AskInput input, CancellationToken ct)
    {
        var question = input.CauHoi.Trim();
        if (question.Length == 0) throw ApiFault.Invalid("Bạn hãy nhập câu hỏi.");
        var history = await conversations.HistoryAsync(userId, id, ct);
        var intent = await localIntent.PredictAsync(question, ct);
        var text = Normalize(question + " " + input.TuKhoa);
        var words = " " + Regex.Replace(text, "[^a-z0-9]+", " ") + " ";
        var wantsPlan = Regex.IsMatch(words, @"\b(lich trinh|hanh trinh|xep lich|len lich|lap lich|lap chuyen|ke hoach)\b");
        var asksSights = Regex.IsMatch(words, @"\b(co nhung|ke ten|danh sach|liet ke|di tich nao|di san nao)\b");
        var asksFood = Regex.IsMatch(words, @"\b(quan an|cho an|an uong)\b");
        var asksLodging = Regex.IsMatch(words, @"\b(khach san|homestay|cho o|luu tru)\b");
        var asksServices = asksFood || asksLodging;
        var region = Region(words);
        if (region is null && input.TinhThanhId is null && input.DiSanId is null && intent == "SUA_HANH_TRINH")
            region = history.Where(x => x.Vai == "USER").Select(x => Region(" " + Normalize(x.NoiDung) + " "))
                .FirstOrDefault(x => x is not null);

        var points = new List<TripPoint>();
        var sources = new List<AssistantReference>();
        string answer;
        if (region is null && input.TinhThanhId is null && input.DiSanId is null)
        {
            answer = "Bạn muốn đi Huế, Hội An, Đà Nẵng hay khu vực nào? Hãy cho mình biết số ngày, số người và ngân sách nếu có.";
        }
        else
        {
            var places = await LoadPlacesAsync(region, input.TinhThanhId, input.DiSanId, ct);
            var isPlan = wantsPlan || (!asksSights && !asksServices &&
                intent is ("LAP_HANH_TRINH" or "LAP_THEO_NGAN_SACH" or "SUA_HANH_TRINH"));
            if (isPlan || asksServices)
            {
                var people = People(words);
                var budget = Budget(text, people);
                var province = places.FirstOrDefault()?.ProvinceId ?? input.TinhThanhId;
                if (province is null && region?.Province is string code)
                {
                    var rows = await sql.QueryAsync("""
                        SELECT TinhThanhId FROM dbo.TinhThanh WHERE Ma=@Ma AND HoatDong=1;
                        """, ct, ("Ma", code));
                    if (rows.Count > 0) province = (int)rows[0]["TinhThanhId"]!;
                }

                var services = province is int provinceId
                    ? await LoadServicesAsync(provinceId, region?.ServiceArea, ct)
                    : new List<Service>();
                var title = region?.Title ?? places.FirstOrDefault()?.ProvinceName ?? "khu vực đã chọn";
                if (isPlan)
                    answer = await BuildPlanAsync(title, input.SoNgay, people, budget, places, services, points, sources, ct);
                else
                {
                    var matches = services.Where(x => x.Type == "QUAN_AN" ? asksFood : asksLodging).Take(10).ToList();
                    answer = matches.Count == 0 ? $"Chưa có dịch vụ phù hợp ở {title}."
                        : $"Dịch vụ tham khảo ở {title}:\n" + string.Join("\n", matches.Select(x =>
                            $"- {x.DisplayName} · " + (x.Price is decimal value
                                ? Money(value) + (x.Type == "QUAN_AN" ? "/người/bữa" : "/phòng/đêm") : "Chưa có giá")));
                    sources.AddRange(matches.Select(x => new AssistantReference("DICH_VU", x.Id, x.DisplayName, null)));
                }
            }
            else
            {
                answer = places.Count == 0 ? "Mình chưa có di sản công khai phù hợp trong dữ liệu."
                    : "Các di sản công khai trong khu vực:\n" + string.Join("\n",
                        places.Take(12).Select(x => $"- {x.Name} — {x.Summary}"));
                sources.AddRange(places.Take(12).Select(PlaceReference));
            }
        }

        var refs = sources.DistinctBy(x => (x.Loai, x.Id)).ToList();
        var result = new GeminiResult(new GeminiReply(answer,
            refs.Select(x => new GeminiCitation(x.Loai, x.Id)).ToList(), points), "intent_tree+sql", null, null);
        var savedId = await conversations.SaveTurnAsync(userId, id, history.FirstOrDefault()?.TinNhanAiId,
            question, result, ct, nhaCungCap: "LOCAL");
        return new AssistantResponse(savedId, answer, refs, points);
    }

    private async Task<List<Place>> LoadPlacesAsync(Area? region, int? province, int? heritage, CancellationToken ct)
    {
        var rows = await sql.QueryAsync("""
            SELECT TOP (14) d.DiSanId,d.TinhThanhId,d.TinhThanh,d.Slug,d.Ten,
                   LEFT(d.TomTat,220) AS TomTat,d.GiaVeThamKhao
            FROM dbo.vw_DiSanCongKhai d
            JOIN dbo.TinhThanh t ON t.TinhThanhId=d.TinhThanhId
            WHERE (@Id IS NULL OR d.DiSanId=@Id) AND (@Province IS NULL OR d.TinhThanhId=@Province)
              AND (@Code IS NULL OR t.Ma=@Code)
              AND (@Slug IS NULL OR d.Slug=@Slug)
              AND (@HoiAn=0 OR d.Slug IN ('pho-co-hoi-an','lang-gom-thanh-ha','thanh-dia-my-son'))
            ORDER BY CASE WHEN d.Slug IN ('dai-noi-hue','pho-co-hoi-an') THEN 0 ELSE 1 END,d.DiSanId;
            """, ct, ("Id", heritage), ("Province", province), ("Code", region?.Province),
            ("Slug", region?.Slug), ("HoiAn", region?.HoiAn == true));
        return rows.Select(x => new Place((int)x["DiSanId"]!, (int)x["TinhThanhId"]!, (string)x["TinhThanh"]!,
            (string)x["Slug"]!, (string)x["Ten"]!, (string?)x["TomTat"], (decimal?)x["GiaVeThamKhao"])).ToList();
    }

    private async Task<List<Service>> LoadServicesAsync(int province, string? area, CancellationToken ct)
    {
        var rows = await sql.RunAsync("dbo.usp_LayDichVuDuLich", ct, ("TinhThanhId", province), ("KhuVuc", area),
            ("GioiHan", 100), ("BaoGomDemo", environment.IsDevelopment()));
        return rows.Select(x => new Service((int)x["DichVuId"]!, (string)x["Ten"]!, (string)x["Loai"]!,
            (decimal?)x["GiaVnd"], (short?)x["SucChuaPhong"], (bool)x["LaDuLieuDemo"]!)).ToList();
    }

    private async Task<string> BuildPlanAsync(string area, int days, int people, decimal? budget,
        IReadOnlyList<Place> places, IReadOnlyList<Service> services, List<TripPoint> points,
        List<AssistantReference> sources, CancellationToken ct)
    {
        if (places.Count == 0 && services.Count == 0)
            return $"Mình chưa có dữ liệu công khai để lập lịch cho {area}.";

        int Rooms(Service row) => (int)Math.Ceiling((decimal)people / row.Capacity!.Value);
        var hotel = services.Where(x => (x.Type is "KHACH_SAN" or "HOMESTAY") && x.Capacity is > 0)
            .OrderBy(x => x.Price is null).ThenBy(x => x.Price * Rooms(x)).FirstOrDefault();
        var restaurant = services.Where(x => x.Type == "QUAN_AN")
            .OrderBy(x => x.Price is null).ThenBy(x => x.Price).FirstOrDefault();

        var answer = new StringBuilder()
            .AppendLine($"{area} · {days} ngày · {people} người")
            .AppendLine("Giờ dưới đây là khung giờ gợi ý. Hãy xác nhận giờ mở cửa và tình trạng còn chỗ.");
        for (var day = 1; day <= days; day++)
        {
            answer.AppendLine($"\nNgày {day}");
            short order = 1;
            void Add(int hour, int minute, string title, string note, int? place, int? activity,
                int? service, decimal? price, int quantity, string unit)
            {
                points.Add(new TripPoint(day, order++, place, activity, new TimeOnly(hour, minute), note,
                    service, price, price is null ? null : quantity, price is null ? null : unit));
                answer.AppendLine($"{hour:00}:{minute:00} · {title}"
                    + (price is decimal known ? $" · {Money(known * quantity)}" : " · Chưa có giá"));
            }
            void Meal(int hour, string label)
            {
                if (restaurant is null) return;
                Add(hour, 0, label + " — " + restaurant.DisplayName, label, null, null,
                    restaurant.Id, restaurant.Price, people, "NGUOI_BUA");
                sources.Add(new AssistantReference("DICH_VU", restaurant.Id, restaurant.DisplayName, null));
            }

            Meal(7, "Ăn sáng");
            var place = places.ElementAtOrDefault(day - 1);
            if (place is not null)
            {
                Add(8, 30, place.Name, "Tham quan di sản", place.Id, null, null, place.Price, people, "NGUOI_LUOT");
                sources.Add(PlaceReference(place));
            }
            else
            {
                answer.AppendLine("08:30 · Còn trống. Chưa có thêm di sản phù hợp trong dữ liệu.");
            }
            Meal(12, "Ăn trưa");

            if (place is not null)
            {
                var activities = await sql.QueryAsync("""
                    SELECT TOP (1) TraiNghiemId,Ten,Slug,GiaMoiNguoi
                    FROM dbo.vw_TraiNghiemCongKhai WHERE DiSanId=@Id
                    ORDER BY CASE WHEN GiaMoiNguoi IS NULL THEN 1 ELSE 0 END,GiaMoiNguoi,TraiNghiemId;
                    """, ct, ("Id", place.Id));
                if (activities.FirstOrDefault() is { } activity)
                {
                    var activityId = (int)activity["TraiNghiemId"]!;
                    var name = (string)activity["Ten"]!;
                    var price = (decimal?)activity["GiaMoiNguoi"];
                    var known = points.Sum(p => (p.DonGiaVnd ?? 0) * (p.SoLuongTinhGia ?? 0));
                    var mealsLeft = (days - day) * 3 + 1;
                    var nightsLeft = Math.Max(0, days - day);
                    var remainingCost = (restaurant?.Price ?? 0) * people * mealsLeft
                        + (hotel is null ? 0 : (hotel.Price ?? 0) * Rooms(hotel) * nightsLeft)
                        + places.Skip(day).Take(days - day).Sum(p => (p.Price ?? 0) * people);
                    // Không đưa hoạt động chưa rõ giá vào lịch có ngân sách.
                    if (budget is null || (price is decimal amount && known + amount * people + remainingCost <= budget.Value))
                    {
                        Add(14, 0, name, "Trải nghiệm đối tác", null, activityId, null, price, people, "NGUOI_LUOT");
                        sources.Add(new AssistantReference("TRAI_NGHIEM", activityId, name, $"/trai-nghiem/{activity["Slug"]}"));
                    }
                }
            }

            if (day < days && hotel is not null)
            {
                Add(17, 0, hotel.DisplayName, day == 1 ? "Nhận phòng" : "Lưu trú đêm tiếp theo",
                    null, null, hotel.Id, hotel.Price, Rooms(hotel), "PHONG_DEM");
                sources.Add(new AssistantReference("DICH_VU", hotel.Id, hotel.DisplayName, null));
            }
            Meal(18, "Ăn tối");
        }

        if (restaurant is null) answer.AppendLine("\nKhu vực chưa có quán ăn để đưa vào lịch.");
        if (days > 1 && hotel is null) answer.AppendLine("\nKhu vực chưa có chỗ ở để đưa vào lịch.");
        var subtotal = points.Sum(p => (p.DonGiaVnd ?? 0) * (p.SoLuongTinhGia ?? 0));
        answer.AppendLine($"\nTạm tính các điểm đã có giá: {Money(subtotal)}.");
        answer.AppendLine("Chưa gồm di chuyển và các điểm chưa có giá. Điểm [DEMO] dùng giá giả lập.");
        if (budget is decimal cap)
            answer.AppendLine(subtotal > cap ? $"Tạm tính vượt ngân sách {Money(subtotal - cap)}."
                : $"Còn {Money(cap - subtotal)} sau các khoản đã có giá, trên ngân sách {Money(cap)} của cả nhóm.");

        if (!TripPlanValidation.IsValid(points, days))
            throw ApiFault.Invalid("Dữ liệu điểm đến chưa phù hợp để tạo lịch.");
        return answer.ToString().TrimEnd();
    }

    private static AssistantReference PlaceReference(Place row) =>
        new("DI_SAN", row.Id, row.Name, $"/di-san/{row.Slug}");

    private static Area? Region(string text) =>
        Regex.IsMatch(text, @"\b(hoi an|quang nam)\b") ? new("Hội An và vùng lân cận", ServiceArea: "Phố cổ Hội An", HoiAn: true)
        : Regex.IsMatch(text, @"\b(thanh ha)\b") ? new("Thanh Hà", Slug: "lang-gom-thanh-ha", ServiceArea: "Làng gốm Thanh Hà")
        : Regex.IsMatch(text, @"\b(my son)\b") ? new("Mỹ Sơn", Slug: "thanh-dia-my-son", ServiceArea: "Thánh địa Mỹ Sơn")
        : Regex.IsMatch(text, @"\b(hue|dai noi)\b") ? new("Huế", Province: "HUE")
        : Regex.IsMatch(text, @"\b(da nang)\b") ? new("Đà Nẵng", Province: "DA_NANG", ServiceArea: "Trung tâm Đà Nẵng")
        : Regex.IsMatch(text, @"\b(bat trang)\b") ? new("Bát Tràng", Slug: "lang-gom-bat-trang", ServiceArea: "Làng gốm Bát Tràng")
        : Regex.IsMatch(text, @"\b(ha noi)\b") ? new("Hà Nội", Province: "HA_NOI") : null;

    private static int People(string text)
    {
        var match = Regex.Match(text, @"\b(\d{1,3}|mot|hai|ba|bon|nam|sau|bay|tam|chin|muoi)\s+nguoi\b");
        if (int.TryParse(match.Groups[1].Value, out var count)) return Math.Clamp(count, 1, 100);
        return match.Groups[1].Value switch
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

    private static decimal? Budget(string text, int people)
    {
        var match = Regex.Match(text, @"(?<![0-9.,])(\d{1,8}(?:[.,]\d{1,2})?)\s*(trieu|nghin|ngan|k)\b");
        decimal amount;
        if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            amount = value * (match.Groups[2].Value == "trieu" ? 1_000_000m : 1_000m);
        }
        else
        {
            match = Regex.Match(text, @"(?<![0-9.,])(\d{1,3}(?:[.,]\d{3}){1,3}|\d{4,12})\s*(vnd|dong|d)\b");
            if (!match.Success || !decimal.TryParse(match.Groups[1].Value.Replace(".", "").Replace(",", ""),
                NumberStyles.None, CultureInfo.InvariantCulture, out amount)) return null;
        }

        return Regex.IsMatch(text, @"\bmoi nguoi\b") ? amount * people : amount;
    }

    private static string Normalize(string value) => new string(value.ToLowerInvariant().Normalize(NormalizationForm.FormD)
        .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
        .Select(ch => ch == 'đ' ? 'd' : ch).ToArray());

    private static string Money(decimal amount) => amount.ToString("N0", Vi) + " đ";
    private sealed record Area(string Title, string? Province = null, string? Slug = null,
        string? ServiceArea = null, bool HoiAn = false);
    private sealed record Place(int Id, int ProvinceId, string ProvinceName, string Slug, string Name, string? Summary, decimal? Price);
    private sealed record Service(int Id, string Name, string Type, decimal? Price, short? Capacity, bool Demo)
    {
        public string DisplayName => Demo ? "[DEMO] " + Name : Name;
    }
}