using System.Drawing;
using System.IO;
using System.Text.Json;

namespace PoePriceRunesOfAldurHelperRu;

internal static class Settings
{
    private static readonly string SettingsPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public static void SaveRegion(Rectangle rect)
    {
        var data = new RegionData { X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height };
        var json = JsonSerializer.Serialize(data);
        File.WriteAllText(SettingsPath, json);
    }

    public static Rectangle? LoadRegion()
    {
        if (!File.Exists(SettingsPath)) return null;
        try
        {
            var json = File.ReadAllText(SettingsPath);
            var data = JsonSerializer.Deserialize<RegionData>(json);
            if (data == null) return null;
            return new Rectangle(data.X, data.Y, data.Width, data.Height);
        }
        catch
        {
            return null;
        }
    }

    private class RegionData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
