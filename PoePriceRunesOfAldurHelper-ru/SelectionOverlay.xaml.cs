using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinPoint = System.Windows.Point;
using ShapeRect = System.Windows.Shapes.Rectangle;

namespace PoePriceRunesOfAldurHelperRu;

public partial class SelectionOverlay : Window
{
    private WinPoint _startPoint;
    private bool _isDragging;
    private ShapeRect? _selectionRect;
    private double _dpiScale = 1.0;

    public event Action<System.Drawing.Rectangle>? AreaSelected;

    public SelectionOverlay()
    {
        InitializeComponent();

        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Left = 0;
        Top = 0;

        using var dc = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
        _dpiScale = dc.DpiX / 96.0;
    }

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(SelectionCanvas);
        _isDragging = true;

        _selectionRect = new ShapeRect
        {
            Stroke = System.Windows.Media.Brushes.White,
            StrokeThickness = 2,
            Fill = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(30, 255, 255, 255))
        };

        Canvas.SetLeft(_selectionRect, _startPoint.X);
        Canvas.SetTop(_selectionRect, _startPoint.Y);
        SelectionCanvas.Children.Add(_selectionRect);
    }

    private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging || _selectionRect == null) return;

        var currentPoint = e.GetPosition(SelectionCanvas);
        double x = Math.Min(_startPoint.X, currentPoint.X);
        double y = Math.Min(_startPoint.Y, currentPoint.Y);
        double w = Math.Abs(currentPoint.X - _startPoint.X);
        double h = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(_selectionRect, x);
        Canvas.SetTop(_selectionRect, y);
        _selectionRect.Width = w;
        _selectionRect.Height = h;
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;

        System.Drawing.Rectangle? result = null;
        if (_selectionRect != null)
        {
            double x = Canvas.GetLeft(_selectionRect) * _dpiScale;
            double y = Canvas.GetTop(_selectionRect) * _dpiScale;
            double w = _selectionRect.Width * _dpiScale;
            double h = _selectionRect.Height * _dpiScale;

            if (w > 20 && h > 20)
                result = new System.Drawing.Rectangle((int)x, (int)y, (int)w, (int)h);
        }

        Close();

        if (result.HasValue)
            AreaSelected?.Invoke(result.Value);
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}
