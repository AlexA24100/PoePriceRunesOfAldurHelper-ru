using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;

namespace PoePriceRunesOfAldurHelperRu;

internal class OcrService
{
    private readonly OcrEngine _engine;

    public OcrService()
    {
        var lang = new Language("ru-RU");
        _engine = OcrEngine.TryCreateFromLanguage(lang)
                  ?? OcrEngine.TryCreateFromUserProfileLanguages();

        if (_engine == null)
            throw new InvalidOperationException(
                "OCR-движок не создан. Убедитесь, что в системе установлен русский языковой пакет.");
    }

    public async Task<OcrResultInfo> RecognizeRegionAsync(Rectangle region)
    {
        using var bitmap = CaptureRegion(region);
        var swBitmap = ConvertToSoftwareBitmap(bitmap);
        var stopwatch = Stopwatch.StartNew();
        var result = await _engine.RecognizeAsync(swBitmap);
        stopwatch.Stop();

        var info = new OcrResultInfo();

        if (result != null)
        {
            foreach (var line in result.Lines)
            {
                double minY = double.MaxValue, maxY = 0;
                string text = "";
                foreach (var word in line.Words)
                {
                    if (text.Length > 0) text += " ";
                    text += word.Text;
                    minY = Math.Min(minY, word.BoundingRect.Y);
                    maxY = Math.Max(maxY, word.BoundingRect.Y + word.BoundingRect.Height);
                }

                info.Lines.Add(new LineInfo
                {
                    Text = text,
                    Y = region.Y + (int)minY,
                    Height = (int)(maxY - minY)
                });
            }
        }

        var texts = info.Lines.Select(l => l.Text).ToList();
        LogService.WriteLog(texts, stopwatch.Elapsed.TotalMilliseconds);
        return info;
    }

    private static Bitmap CaptureRegion(Rectangle region)
    {
        var bitmap = new Bitmap(region.Width, region.Height);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(region.X, region.Y, 0, 0, region.Size);
        return bitmap;
    }

    private static SoftwareBitmap ConvertToSoftwareBitmap(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        var rect = new Rectangle(0, 0, width, height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        int stride = Math.Abs(data.Stride);
        int rowBytes = width * 4;
        byte[] pixels = new byte[height * rowBytes];

        if (stride == rowBytes)
        {
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        }
        else
        {
            for (int y = 0; y < height; y++)
            {
                IntPtr srcRow = IntPtr.Add(data.Scan0, y * stride);
                Marshal.Copy(srcRow, pixels, y * rowBytes, rowBytes);
            }
        }

        bitmap.UnlockBits(data);

        var swBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
            width, height, BitmapAlphaMode.Ignore);

        var buffer = CryptographicBuffer.CreateFromByteArray(pixels);
        swBitmap.CopyFromBuffer(buffer);

        return swBitmap;
    }
}
