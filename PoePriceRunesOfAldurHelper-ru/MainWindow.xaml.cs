using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace PoePriceRunesOfAldurHelperRu;

public partial class MainWindow : Window
{
    private HwndSource? _source;
    private bool _recognitionActive;
    private bool _ocrBusy;
    private DispatcherTimer? _ocrTimer;
    private OcrService? _ocrService;
    private PriceDatabase _priceDb = new();
    private List<TextOverlay> _lineOverlays = [];
    private double _dpiScale = 1.0;

    private static readonly string All1Path =
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\all1.md"));

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;

        using var dc = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
        _dpiScale = dc.DpiX / 96.0;

        _ocrService = new OcrService();
        _ocrTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _ocrTimer.Tick += OcrTimerTick;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _source = PresentationSource.FromVisual(this) as HwndSource;
        if (_source != null)
            _source.AddHook(WndProc);

        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.RegisterHotKey(handle, NativeMethods.HOTKEY_F6_ID,
            NativeMethods.MOD_NONE, NativeMethods.VK_F6);
        NativeMethods.RegisterHotKey(handle, NativeMethods.HOTKEY_F7_ID,
            NativeMethods.MOD_NONE, NativeMethods.VK_F7);
        NativeMethods.RegisterHotKey(handle, NativeMethods.HOTKEY_F8_ID,
            NativeMethods.MOD_NONE, NativeMethods.VK_F8);

        LoadPricesFromFile();

        var region = Settings.LoadRegion();
        if (region.HasValue)
        {
            var r = region.Value;
            TxtRegion.Text = $"Область: ({r.X}, {r.Y}) {r.Width}x{r.Height}";
            BtnStartRecognition.IsEnabled = true;
        }

        _ = TryUpdatePricesOnStartupAsync();
    }

    private void LoadPricesFromFile()
    {
        _priceDb.Load(All1Path);
        TxtPriceDate.Text = _priceDb.Count > 0
            ? $"Цены ({_priceDb.Count} предм.): обновлены {_priceDb.LastUpdateDate}"
            : "Цены: не загружены";
    }

    private async Task TryUpdatePricesOnStartupAsync()
    {
        BtnUpdatePrices.IsEnabled = false;
        TxtPriceDate.Text = "Цены: обновление...";

        var success = await PriceUpdater.UpdatePricesAsync();

        LoadPricesFromFile();

        if (!success && _priceDb.Count > 0)
            TxtPriceDate.Text = $"Цены ({_priceDb.Count} предм.): сайт недоступен, оставлены от {_priceDb.LastUpdateDate}";

        BtnUpdatePrices.IsEnabled = true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (id == NativeMethods.HOTKEY_F6_ID)
                _ = UpdatePricesAsync();
            else if (id == NativeMethods.HOTKEY_F7_ID)
                _ = SelectAreaAsync();
            else if (id == NativeMethods.HOTKEY_F8_ID)
                ToggleRecognition();
        }
        return IntPtr.Zero;
    }

    private void BtnSelectArea_Click(object sender, RoutedEventArgs e)
    {
        _ = SelectAreaAsync();
    }

    private async Task SelectAreaAsync()
    {
        Rectangle? result = null;
        var overlay = new SelectionOverlay();
        overlay.AreaSelected += rect => result = rect;
        overlay.ShowDialog();

        if (result.HasValue)
        {
            Settings.SaveRegion(result.Value);
            var r = result.Value;
            TxtRegion.Text = $"Область: ({r.X}, {r.Y}) {r.Width}x{r.Height}";
            BtnStartRecognition.IsEnabled = true;
        }
    }

    private async void BtnUpdatePrices_Click(object? sender, RoutedEventArgs e)
    {
        await UpdatePricesAsync();
    }

    private async Task UpdatePricesAsync()
    {
        BtnUpdatePrices.IsEnabled = false;
        BtnUpdatePrices.Content = "Обновление...";
        TxtPriceDate.Text = "Цены: обновление...";

        var success = await PriceUpdater.UpdatePricesAsync();
        LoadPricesFromFile();

        if (!success)
            TxtPriceDate.Text = "Цены: ошибка подключения к poe.ninja";

        BtnUpdatePrices.Content = "Обновить цены (F6)";
        BtnUpdatePrices.IsEnabled = true;
    }

    private void BtnStartRecognition_Click(object? sender, RoutedEventArgs e)
    {
        ToggleRecognition();
    }

    private void ToggleRecognition()
    {
        _recognitionActive = !_recognitionActive;
        if (_recognitionActive)
        {
            var region = Settings.LoadRegion();
            if (!region.HasValue)
            {
                _recognitionActive = false;
                return;
            }

            _ocrTimer?.Start();
            BtnStartRecognition.Content = "Остановить распознавание (F8)";
            TxtStatus.Text = "Статус: распознавание активно";
        }
        else
        {
            _ocrTimer?.Stop();
            BtnStartRecognition.Content = "Запустить распознавание (F8)";
            TxtStatus.Text = "Статус: остановлено";
            CloseLineOverlays();
        }
    }

    private void CloseLineOverlays()
    {
        foreach (var ol in _lineOverlays)
            ol.Close();
        _lineOverlays.Clear();
    }

    private async void OcrTimerTick(object? sender, EventArgs e)
    {
        if (_ocrBusy) return;
        _ocrBusy = true;
        try
        {
            var region = Settings.LoadRegion();
            if (!region.HasValue) return;

            var result = await _ocrService!.RecognizeRegionAsync(region.Value);
            var lines = result.Lines;

            CloseLineOverlays();

            if (lines.Count > 0)
            {
                var matches = lines.Select(l => (line: l, match: _priceDb.FindMatch(l.Text))).ToList();
                var maxTotal = matches
                    .Where(x => x.match.Info?.PriceDivine.HasValue == true)
                    .DefaultIfEmpty()
                    .MaxBy(x => x.match.Info?.PriceDivine.GetValueOrDefault() * x.match.Quantity ?? 0);

                foreach (var (l, m) in matches)
                {
                    bool isTop = maxTotal.match != null && m.Info?.PriceDivine.HasValue == true
                        && Math.Abs((m.Info.PriceDivine.Value * m.Quantity) -
                                    (maxTotal.match.Info?.PriceDivine.GetValueOrDefault() * maxTotal.match.Quantity ?? 0)) < 0.001;
                    var overlay = new TextOverlay(m, l.Y, region.Value, _dpiScale, isTop);
                    overlay.Show();
                    _lineOverlays.Add(overlay);
                }
            }
            else
            {
                var emptyMatch = new PriceDatabase.MatchResult { OriginalText = "— ничего не распознано —" };
                var overlay = new TextOverlay(emptyMatch, 0, region.Value, _dpiScale, false);
                overlay.Show();
                _lineOverlays.Add(overlay);
            }
        }
        catch (Exception ex)
        {
            LogService.WriteError(ex.ToString());
        }
        finally
        {
            _ocrBusy = false;
        }
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
        e.Handled = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _ocrTimer?.Stop();
        _ocrTimer = null;
        CloseLineOverlays();

        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.UnregisterHotKey(handle, NativeMethods.HOTKEY_F6_ID);
        NativeMethods.UnregisterHotKey(handle, NativeMethods.HOTKEY_F7_ID);
        NativeMethods.UnregisterHotKey(handle, NativeMethods.HOTKEY_F8_ID);
    }
}
