using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace PoePriceRunesOfAldurHelperRu;

internal static class PriceUpdater
{
    private static readonly string BasePath =
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));

    private static readonly string All1Path = Path.Combine(BasePath, "all1.md");

    private static readonly string[] ApiCategories =
    [
        "Currency",
        "Runes",
        "Expedition",
        "Verisium",
        "UncutGems"
    ];

    private static readonly string ApiUrl =
        "https://poe.ninja/poe2/api/economy/exchange/current/overview?league=Runes+of+Aldur&type=";

    public static async Task<bool> UpdatePricesAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            var priceMap = new Dictionary<string, PriceData>();

            foreach (var cat in ApiCategories)
            {
                try
                {
                    var url = ApiUrl + cat;
                    var json = await http.GetStringAsync(url);

                    using var doc = JsonDocument.Parse(json);
                    JsonElement.ArrayEnumerator enumerator;

                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("lines", out var linesProp))
                    {
                        enumerator = linesProp.EnumerateArray();
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        enumerator = doc.RootElement.EnumerateArray();
                    }
                    else
                    {
                        continue;
                    }

                    foreach (var item in enumerator)
                    {
                        var eid = item.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                        var pv = item.TryGetProperty("primaryValue", out var pvProp) ? pvProp.GetDouble() : (double?)null;
                        var mvc = item.TryGetProperty("maxVolumeCurrency", out var mvcProp) ? mvcProp.GetString() ?? "" : "";
                        var mvr = item.TryGetProperty("maxVolumeRate", out var mvrProp) ? mvrProp.GetDouble() : (double?)null;

                        if (!string.IsNullOrEmpty(eid) && pv.HasValue)
                        {
                            priceMap[eid] = new PriceData { PrimaryValue = pv.Value, MaxVolumeCurrency = mvc, MaxVolumeRate = mvr };
                        }
                    }
                }
                catch
                {
                }
            }

            if (priceMap.Count == 0)
                return false;

            UpdateAll1File(priceMap);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void UpdateAll1File(Dictionary<string, PriceData> priceMap)
    {
        if (!File.Exists(All1Path)) return;

        var lines = File.ReadAllLines(All1Path);
        var newLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                newLines.Add(line);
                continue;
            }

            var parts = trimmed.Split(" ## ");
            if (parts.Length < 2)
            {
                newLines.Add(line);
                continue;
            }

            var ru = parts[0].Trim();
            var rest = parts[1].Trim();

            string eng;
            if (rest.Contains(" ## "))
                eng = rest.Split(" ## ")[0].Trim();
            else
                eng = rest.TrimEnd('#').Trim();

            if (string.IsNullOrEmpty(eng) || !priceMap.TryGetValue(eng, out var pd))
            {
                newLines.Add(line);
                continue;
            }

            var suffix = MakePriceSuffix(pd);
            newLines.Add($"{ru} ## {eng} ## {suffix}");
        }

        File.WriteAllLines(All1Path, newLines);
    }

    private static string MakePriceSuffix(PriceData pd)
    {
        var priceDivine = FormatPrice(pd.PrimaryValue);

        if (pd.MaxVolumeCurrency == "divine")
            return $"{priceDivine} divine";

        double? priceMvc = pd.MaxVolumeRate.HasValue && pd.MaxVolumeRate.Value > 0
            ? 1.0 / pd.MaxVolumeRate.Value
            : null;

        var priceMvcStr = priceMvc.HasValue ? FormatPrice(priceMvc.Value) : "?";
        return $"{priceMvcStr} {pd.MaxVolumeCurrency} ## {priceDivine} divine";
    }

    private static string FormatPrice(double val)
    {
        if (val < 0.01) return "<0.01";
        if (val >= 100) return Math.Round(val).ToString(CultureInfo.InvariantCulture);
        if (val >= 10) return Math.Round(val, 1).ToString("F1", CultureInfo.InvariantCulture);
        return Math.Round(val, 2).ToString("F2", CultureInfo.InvariantCulture);
    }

    private class PriceData
    {
        public double PrimaryValue { get; init; }
        public string MaxVolumeCurrency { get; init; } = "";
        public double? MaxVolumeRate { get; init; }
    }
}
