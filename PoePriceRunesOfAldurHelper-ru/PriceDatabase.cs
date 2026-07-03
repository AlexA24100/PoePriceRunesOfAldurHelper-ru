using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace PoePriceRunesOfAldurHelperRu;

public class PriceDatabase
{
    public class PriceInfo
    {
        public string RussianName { get; init; } = "";
        public string EnglishId { get; init; } = "";
        public double? PriceChaos { get; set; }
        public double? PriceExalted { get; set; }
        public double? PriceDivine { get; set; }

        public bool HasAnyPrice => PriceChaos.HasValue || PriceExalted.HasValue || PriceDivine.HasValue;
    }

    public class MatchResult
    {
        public PriceInfo? Info { get; init; }
        public int Quantity { get; init; } = 1;
        public string? MatchedName { get; init; }
        public string OriginalText { get; init; } = "";
        public bool IsFuzzy { get; init; }
    }

    private List<PriceInfo> _items = [];
    private static readonly Regex QtyRegex = new(@"\s*\((\d+)\)\s*$", RegexOptions.Compiled);
    private static readonly Regex ParensRegex = new(@"\([^)]*\)", RegexOptions.Compiled);
    private static readonly Regex GarbageRegex = new(@"[^\w\s\(\)\-\–\—\:\«\»]", RegexOptions.Compiled);

    public string? LastUpdateDate { get; private set; }
    public int Count => _items.Count;

    public void Load(string all1Path)
    {
        if (!File.Exists(all1Path))
        {
            _items = [];
            LastUpdateDate = null;
            return;
        }

        var items = new List<PriceInfo>();
        var lines = File.ReadAllLines(all1Path);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(" ## ");
            if (parts.Length < 2) continue;

            var ruName = parts[0].Trim();
            var engId = parts[1].Trim();

            var info = new PriceInfo
            {
                RussianName = ruName,
                EnglishId = engId
            };

            for (int i = 2; i < parts.Length; i++)
            {
                var priceStr = parts[i].Trim();
                if (string.IsNullOrEmpty(priceStr)) continue;

                var tokens = priceStr.Split(' ');
                if (tokens.Length < 2) continue;

                var currency = tokens[^1];
                var priceToken = string.Join(' ', tokens[..^1]);

                if (double.TryParse(priceToken, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                {
                    switch (currency)
                    {
                        case "chaos": info.PriceChaos = val; break;
                        case "exalted": info.PriceExalted = val; break;
                        case "divine": info.PriceDivine = val; break;
                    }
                }
            }

            items.Add(info);
        }

        _items = items;

        var lastWrite = File.GetLastWriteTime(all1Path);
        LastUpdateDate = lastWrite.ToString("yyyy-MM-dd HH:mm");
    }

    public MatchResult FindMatch(string ocrText)
    {
        var original = (ocrText ?? "").Trim();
        if (string.IsNullOrEmpty(original))
            return new MatchResult { OriginalText = original };

        var qtyMatch = QtyRegex.Match(original);
        int quantity = 1;
        var searchText = original;
        if (qtyMatch.Success)
        {
            quantity = int.Parse(qtyMatch.Groups[1].Value);
            searchText = original[..qtyMatch.Index].Trim();
        }

        if (string.IsNullOrEmpty(searchText))
            return new MatchResult { OriginalText = original, Quantity = quantity };

        var cleaned = CleanText(searchText);

        if (cleaned.Length < 3)
            return new MatchResult { OriginalText = original, Quantity = quantity };

        var exact = _items.FirstOrDefault(i =>
            string.Equals(CleanText(i.RussianName), cleaned, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return new MatchResult { Info = exact, Quantity = quantity, MatchedName = exact.RussianName, OriginalText = original };

        var searchStripped = ParensRegex.Replace(searchText, "").Trim();

        foreach (var item in _items)
        {
            var itemStripped = ParensRegex.Replace(item.RussianName, "").Trim();
            if (string.Equals(searchStripped, itemStripped, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(CleanText(searchStripped), CleanText(itemStripped), StringComparison.OrdinalIgnoreCase))
            {
                return new MatchResult { Info = item, Quantity = quantity, MatchedName = item.RussianName, OriginalText = original };
            }
        }

        PriceInfo? bestMatch = null;
        double bestScore = 0.55;
        var searchNorm = CleanText(searchStripped);

        foreach (var item in _items)
        {
            var itemNorm = CleanText(ParensRegex.Replace(item.RussianName, "").Trim());
            if (string.IsNullOrEmpty(itemNorm)) continue;

            double score = CalcSimilarity(searchNorm, itemNorm);
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = item;
            }
        }

        if (bestMatch != null)
            return new MatchResult { Info = bestMatch, Quantity = quantity, MatchedName = bestMatch.RussianName, OriginalText = original, IsFuzzy = true };

        return new MatchResult { OriginalText = original, Quantity = quantity };
    }

    private static string CleanText(string s)
    {
        s = s.ToLowerInvariant().Trim();
        s = GarbageRegex.Replace(s, "");
        s = Regex.Replace(s, @"\s+", " ");
        return s.Trim();
    }

    private static double CalcSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

        int dist = LevenshteinDistance(a, b);
        return 1.0 - (double)dist / Math.Max(a.Length, b.Length);
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int m = a.Length, n = b.Length;
        var d = new int[m + 1, n + 1];
        for (int i = 0; i <= m; i++) d[i, 0] = i;
        for (int j = 0; j <= n; j++) d[0, j] = j;
        for (int i = 1; i <= m; i++)
            for (int j = 1; j <= n; j++)
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + (char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1));
        return d[m, n];
    }
}
