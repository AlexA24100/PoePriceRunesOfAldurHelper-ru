using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PoePriceRunesOfAldurHelperRu;

public partial class TextOverlay : Window
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int GWL_EXSTYLE = -20;

    private static readonly BitmapImage IconChaos = LoadIcon("Chaos");
    private static readonly BitmapImage IconExalted = LoadIcon("Exalted");
    private static readonly BitmapImage IconDivine = LoadIcon("divine");

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public TextOverlay(PriceDatabase.MatchResult match, double yCoord, System.Drawing.Rectangle captureRegion, double dpiScale, bool isTopPrice = false)
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;

        double margin = 10;
        double captureRight = captureRegion.Right / dpiScale;
        double captureLeft = captureRegion.Left / dpiScale;
        double screenRight = SystemParameters.PrimaryScreenWidth;

        var displayText = match.MatchedName ?? match.OriginalText;

        double x, y = yCoord / dpiScale;

        if (captureRight + margin + 50 < screenRight)
            x = captureRight + margin;
        else
            x = captureLeft - margin - 400;

        x = Math.Max(0, x);
        y = Math.Max(0, y);

        Left = x;
        Top = y;

        BuildContent(match, displayText, isTopPrice);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE);
    }

    private void BuildContent(PriceDatabase.MatchResult match, string displayText, bool isTopPrice)
    {
        ContentPanel.Children.Clear();
        var priceColor = isTopPrice ? Brushes.Gold : Brushes.White;
        double nameFontSize = isTopPrice ? 20 : 16;
        double iconSize = isTopPrice ? 22 : 18;
        double priceFontSize = isTopPrice ? 17 : 14;

        if (isTopPrice)
            TextBorder.Background = new SolidColorBrush(Color.FromArgb(0xB0, 0x80, 0x00, 0x00));

        if (match.Info != null && match.Info.HasAnyPrice)
        {
            AddPriceIcon(IconDivine, match.Info.PriceDivine, match.Quantity, iconSize, priceFontSize);

            if (match.Info.PriceChaos.HasValue)
                AddPriceIcon(IconChaos, match.Info.PriceChaos, match.Quantity, iconSize, priceFontSize);

            if (match.Info.PriceExalted.HasValue)
                AddPriceIcon(IconExalted, match.Info.PriceExalted, match.Quantity, iconSize, priceFontSize);

            var sep = new TextBlock
            {
                Text = "│",
                Foreground = isTopPrice ? Brushes.Gold : Brushes.Gray,
                FontSize = nameFontSize,
                Margin = new Thickness(6, 1, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            ContentPanel.Children.Add(sep);
        }

        var textBlock = new TextBlock
        {
            Text = displayText,
            FontSize = nameFontSize,
            Foreground = priceColor,
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center
        };

        ContentPanel.Children.Add(textBlock);
    }

    private void AddPriceIcon(BitmapImage icon, double? price, int quantity, double iconSize, double priceFontSize)
    {
        if (!price.HasValue) return;

        var stack = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            Margin = new Thickness(0, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var img = new System.Windows.Controls.Image
        {
            Source = icon,
            Width = iconSize,
            Height = iconSize,
            Margin = new Thickness(0, 0, 3, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var total = Math.Round(price.Value * quantity, 2);
        var priceStr = total >= 100 ? total.ToString("F0") :
                       total >= 10 ? total.ToString("F1") :
                       total < 0.01 ? "<0.01" : total.ToString("F2");

        var tb = new TextBlock
        {
            Text = priceStr,
            FontSize = priceFontSize,
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center
        };

        stack.Children.Add(img);
        stack.Children.Add(tb);
        ContentPanel.Children.Add(stack);
    }

    private static BitmapImage LoadIcon(string name)
    {
        var uri = new Uri($"pack://application:,,,/Icons/{name}.png", UriKind.Absolute);
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource = uri;
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }
}
