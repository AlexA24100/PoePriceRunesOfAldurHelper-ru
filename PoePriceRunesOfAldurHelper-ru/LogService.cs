using System.IO;

namespace PoePriceRunesOfAldurHelperRu;

internal static class LogService
{
    private static readonly string LogPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recognition_log.txt");
    private static readonly object Lock = new();

    public static void WriteLog(IReadOnlyList<string> lines, double elapsedMs = 0)
    {
        lock (Lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var ms = elapsedMs > 0 ? $" ({elapsedMs:F1}ms)" : "";
            var entries = string.Join(" | ", lines);
            File.AppendAllText(LogPath, $"[{timestamp}]{ms} {entries}\r\n");
        }
    }

    public static void WriteError(string message)
    {
        lock (Lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            File.AppendAllText(LogPath, $"[{timestamp}] ERROR: {message}\r\n");
        }
    }
}
