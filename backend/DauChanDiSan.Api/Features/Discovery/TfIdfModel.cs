using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DauChanDiSan.Api.Features.Discovery;

public sealed partial class TfIdfModel
{
    private readonly Dictionary<string, double> idf;
    private readonly Dictionary<int, Dictionary<string, double>> documents;

    private TfIdfModel(Dictionary<string, double> idf, Dictionary<int, Dictionary<string, double>> documents)
    {
        this.idf = idf;
        this.documents = documents;
    }

    public static TfIdfModel Fit(IReadOnlyDictionary<int, string> catalog)
    {
        var tokens = catalog.ToDictionary(x => x.Key, x => Tokens(x.Value));
        var frequencies = tokens.Values.SelectMany(x => x.Distinct())
            .GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        var idf = frequencies.ToDictionary(x => x.Key, x => Math.Log((catalog.Count + 1d) / (x.Value + 1d)) + 1);
        return new TfIdfModel(idf, tokens.ToDictionary(x => x.Key, x => Vector(x.Value, idf)));
    }

    public IReadOnlyDictionary<int, double> Score(string profile)
    {
        var query = Vector(Tokens(profile), idf);
        var queryLength = Math.Sqrt(query.Values.Sum(x => x * x));
        return documents.ToDictionary(x => x.Key, x => Cosine(x.Value));

        double Cosine(Dictionary<string, double> doc)
        {
            var docLength = Math.Sqrt(doc.Values.Sum(x => x * x));
            if (queryLength == 0 || docLength == 0) return 0;
            var dot = query.Sum(x => x.Value * doc.GetValueOrDefault(x.Key));
            return Math.Clamp(dot / (queryLength * docLength), 0, 1);
        }
    }

    private static Dictionary<string, double> Vector(IEnumerable<string> words, Dictionary<string, double> weights) =>
        words.Where(weights.ContainsKey).GroupBy(x => x)
            .ToDictionary(x => x.Key, x => (1 + Math.Log(x.Count())) * weights[x.Key]);

    private static string[] Tokens(string text)
    {
        var normalized = new string(text.ToLowerInvariant().Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).Replace('đ', 'd');
        return TokenPattern().Matches(normalized).Select(m => m.Value).ToArray();
    }

    [GeneratedRegex(@"[\p{L}\p{Nd}]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
