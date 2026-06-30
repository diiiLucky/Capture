using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using CodexCapture.Interop;
using CodexCapture.Models;
using CodexCapture.Theme;
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;

namespace CodexCapture.Windows;

public sealed class CaptureReviewOverlayWindow : Window
{
    private readonly CaptureResult _capture;
    private readonly Canvas _canvas = new();
    private readonly Border _selection = new();

    public CaptureReviewOverlayWindow(CaptureResult capture)
    {
        _capture = capture;

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = ThemeBrushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        ShowActivated = false;
        IsHitTestVisible = false;

        var bounds = GetVirtualScreenBounds();
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;

        Content = _canvas;
        SourceInitialized += (_, _) => MakeWindowClickThrough();
        Loaded += (_, _) => DrawReviewOverlay();
    }

    private void MakeWindowClickThrough()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
        var updatedStyle = style |
                           NativeMethods.WsExTransparent |
                           NativeMethods.WsExNoActivate |
                           NativeMethods.WsExToolWindow;
        _ = NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExStyle, (nint)updatedStyle);
    }

    private void DrawReviewOverlay()
    {
        var selectionRect = ToLocalRect(new Int32Rect(
            _capture.ScreenX,
            _capture.ScreenY,
            _capture.ScreenWidth,
            _capture.ScreenHeight));

        DrawDimRegions(selectionRect);
        DrawSelection(selectionRect);
        DrawHandles(selectionRect);
    }

    private void DrawDimRegions(WpfRect selectionRect)
    {
        var dimBrush = ThemeBrushes.OverlayDim;

        AddDimRect(0, 0, Width, Math.Max(0, selectionRect.Top), dimBrush);
        AddDimRect(0, selectionRect.Bottom, Width, Math.Max(0, Height - selectionRect.Bottom), dimBrush);
        AddDimRect(0, selectionRect.Top, Math.Max(0, selectionRect.Left), selectionRect.Height, dimBrush);
        AddDimRect(selectionRect.Right, selectionRect.Top, Math.Max(0, Width - selectionRect.Right), selectionRect.Height, dimBrush);
    }

    private void DrawSelection(WpfRect rect)
    {
        _selection.BorderBrush = ThemeBrushes.Ink;
        _selection.BorderThickness = new Thickness(1);
        _selection.Background = ThemeBrushes.SelectionReviewFill;
        _selection.CornerRadius = ThemeBrushes.Radius.XSmall;
        SetElementBounds(_selection, rect);
        _canvas.Children.Add(_selection);

        var dashed = new Rectangle
        {
            Stroke = ThemeBrushes.SelectionBorder,
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 2, 3 },
            Fill = ThemeBrushes.Transparent,
            Width = rect.Width,
            Height = rect.Height
        };
        Canvas.SetLeft(dashed, rect.Left);
        Canvas.SetTop(dashed, rect.Top);
        _canvas.Children.Add(dashed);
    }

    private void DrawHandles(WpfRect rect)
    {
        var points = new[]
        {
            new WpfPoint(rect.Left, rect.Top),
            new WpfPoint(rect.Left + rect.Width / 2, rect.Top),
            new WpfPoint(rect.Right, rect.Top),
            new WpfPoint(rect.Left, rect.Top + rect.Height / 2),
            new WpfPoint(rect.Right, rect.Top + rect.Height / 2),
            new WpfPoint(rect.Left, rect.Bottom),
            new WpfPoint(rect.Left + rect.Width / 2, rect.Bottom),
            new WpfPoint(rect.Right, rect.Bottom)
        };

        foreach (var point in points)
        {
            var handle = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = ThemeBrushes.HandleFill,
                Stroke = ThemeBrushes.HandleStroke,
                StrokeThickness = 1,
                Effect = ThemeBrushes.Shadow.XS
            };
            Canvas.SetLeft(handle, point.X - handle.Width / 2);
            Canvas.SetTop(handle, point.Y - handle.Height / 2);
            _canvas.Children.Add(handle);
        }
    }

    private void AddDimRect(double left, double top, double width, double height, Brush brush)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = brush
        };
        Canvas.SetLeft(rect, left);
        Canvas.SetTop(rect, top);
        _canvas.Children.Add(rect);
    }

    private WpfRect ToLocalRect(Int32Rect screenRect)
    {
        var topLeft = PointFromScreen(new WpfPoint(screenRect.X, screenRect.Y));
        var bottomRight = PointFromScreen(new WpfPoint(screenRect.X + screenRect.Width, screenRect.Y + screenRect.Height));
        return new WpfRect(topLeft, bottomRight);
    }

    private static void SetElementBounds(FrameworkElement element, WpfRect rect)
    {
        Canvas.SetLeft(element, rect.Left);
        Canvas.SetTop(element, rect.Top);
        element.Width = rect.Width;
        element.Height = rect.Height;
    }

    private static WpfRect GetVirtualScreenBounds() => new(
        SystemParameters.VirtualScreenLeft,
        SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth,
        SystemParameters.VirtualScreenHeight);
}
