using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CodexCapture.Interop;
using CodexCapture.Models;
using CodexCapture.Services;
using CodexCapture.Theme;
using SymbolIcon = Wpf.Ui.Controls.SymbolIcon;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace CodexCapture.Windows;

public sealed class CaptureMenuWindow : Window
{
    private const double CaptureGap = 8;
    private const double ScreenMargin = 8;
    private const double InsideMargin = 12;

    private readonly CaptureResult _capture;
    private readonly ScreenCaptureService _screenCaptureService;
    private readonly AiTranslationService _translationService;
    private readonly CodexImportService _codexImportService;
    private readonly SettingsService _settingsService;
    private readonly LogService _logService;
    private readonly Action _closeReviewOverlay;

    private readonly List<UIElement> _allButtons = new();
    private readonly List<Border> _allSeparators = new();
    private bool _isClosing;
    private bool _isLoading;

    public CaptureMenuWindow(
        CaptureResult capture,
        ScreenCaptureService screenCaptureService,
        AiTranslationService translationService,
        CodexImportService codexImportService,
        SettingsService settingsService,
        LogService logService,
        Action closeReviewOverlay)
    {
        _capture = capture;
        _screenCaptureService = screenCaptureService;
        _translationService = translationService;
        _codexImportService = codexImportService;
        _settingsService = settingsService;
        _logService = logService;
        _closeReviewOverlay = closeReviewOverlay;

        Title = "截图操作";
        SizeToContent = SizeToContent.WidthAndHeight;
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = ThemeBrushes.Transparent;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Opacity = 0;
        Focusable = true;

        Content = BuildContent();
        Loaded += (_, _) =>
        {
            PositionNearCapture();
            Dispatcher.BeginInvoke(PositionNearCapture, DispatcherPriority.ContextIdle);
            AnimateIn();
            BringToFront();
        };
        PreviewKeyDown += OnPreviewKeyDown;
        Closing += (_, e) =>
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                AnimateOut();
            }
        };
        Closed += (_, _) => SafeCloseReviewOverlay();
    }

    private UIElement BuildContent()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(8)
        };

        panel.Children.Add(AddButton(MenuButton("复制", SymbolRegular.Copy20, ThemeBrushes.Primary, btn => RunAndClose(OnCopy))));
        panel.Children.Add(AddSeparator());
        panel.Children.Add(AddButton(MenuButton("翻译", SymbolRegular.Translate20, ThemeBrushes.Primary, btn => RunLoadingAction(btn, OnTranslateAsync))));
        panel.Children.Add(AddSeparator());
        panel.Children.Add(AddButton(MenuButton("Codex", SymbolRegular.Chat20, ThemeBrushes.Primary, btn => RunLoadingAction(btn, OnAskCodexAsync))));
        panel.Children.Add(AddSeparator());
        panel.Children.Add(AddButton(MenuButton("打开", SymbolRegular.Open20, ThemeBrushes.Primary, btn => RunAndClose(OnOpenFile))));
        panel.Children.Add(AddSeparator());
        panel.Children.Add(AddButton(MenuButton("取消", SymbolRegular.Dismiss20, ThemeBrushes.Error, _ => CloseWithReview())));

        var outerBorder = new Border
        {
            Background = ThemeBrushes.FrostedMedium,
            BorderBrush = ThemeBrushes.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = ThemeBrushes.Radius.XL,
            Child = panel,
            Effect = ThemeBrushes.Shadow.LG,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1.0, 1.0)
        };

        return outerBorder;
    }

    private UIElement AddButton(UIElement button)
    {
        _allButtons.Add(button);
        return button;
    }

    private Border AddSeparator()
    {
        var sep = VerticalSeparator();
        _allSeparators.Add(sep);
        return sep;
    }

    private UIElement MenuButton(string label, SymbolRegular icon, Brush iconColor, Action<System.Windows.Controls.Button> onClick)
    {
        var iconElement = new SymbolIcon
        {
            Symbol = icon,
            FontSize = 22,
            Foreground = iconColor,
            Margin = new Thickness(0, 0, 0, 4),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform(0)
        };

        var labelElement = new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = ThemeBrushes.TextSecondary,
            TextAlignment = TextAlignment.Center
        };

        var content = new StackPanel
        {
            Width = 56,
            Height = 52,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(iconElement);
        content.Children.Add(labelElement);

        var normalBg = ThemeBrushes.Transparent;
        var hoverBg = ThemeBrushes.PrimarySubtle;

        var button = new System.Windows.Controls.Button
        {
            Content = content,
            Tag = new ButtonState { Icon = iconElement, Label = labelElement, OriginalIcon = icon, OriginalLabel = label, OriginalColor = iconColor },
            Width = 60,
            Height = 60,
            Padding = new Thickness(0),
            Margin = new Thickness(2, 0, 2, 0),
            BorderThickness = new Thickness(0),
            Background = normalBg,
            Cursor = System.Windows.Input.Cursors.Hand,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1.0, 1.0)
        };

        button.MouseEnter += (_, _) =>
        {
            if (_isLoading) return;
            button.Background = hoverBg;
            if (iconElement.RenderTransform is ScaleTransform iconSt)
                AnimateIconScale(iconSt, 1.08);
        };
        button.MouseLeave += (_, _) =>
        {
            if (_isLoading) return;
            button.Background = normalBg;
            if (iconElement.RenderTransform is ScaleTransform iconSt)
                AnimateIconScale(iconSt, 1.0);
        };
        button.Click += (_, _) =>
        {
            if (_isLoading) return;
            onClick(button);
        };

        return button;
    }

    private static Border VerticalSeparator() => new()
    {
        Width = 1,
        Height = 24,
        Margin = new Thickness(4, 0, 4, 0),
        Background = ThemeBrushes.BorderSubtle,
        VerticalAlignment = VerticalAlignment.Center
    };

    #region Loading State

    private void SetLoading(System.Windows.Controls.Button activeButton, string loadingLabel)
    {
        _isLoading = true;
        var state = (ButtonState)activeButton.Tag;

        // Swap to spinner icon + rotation animation
        state.Icon.Symbol = SymbolRegular.ArrowSync20;
        state.Icon.Foreground = ThemeBrushes.Primary;
        state.Icon.RenderTransform = new RotateTransform(0);
        StartSpinAnimation(state.Icon);

        // Update label
        state.Label.Text = loadingLabel;

        // Highlight active button
        activeButton.Background = ThemeBrushes.PrimarySubtle;

        // Dim other buttons and separators
        foreach (var btn in _allButtons)
        {
            if (btn != activeButton)
            {
                btn.IsHitTestVisible = false;
                var fadeOut = new DoubleAnimation(1, 0.3, ThemeBrushes.Duration.Hover)
                {
                    EasingFunction = ThemeBrushes.Easing.Hover
                };
                btn.BeginAnimation(OpacityProperty, fadeOut);
            }
        }
        foreach (var sep in _allSeparators)
        {
            var fadeOut = new DoubleAnimation(1, 0.3, ThemeBrushes.Duration.Hover)
            {
                EasingFunction = ThemeBrushes.Easing.Hover
            };
            sep.BeginAnimation(OpacityProperty, fadeOut);
        }
    }

    private void SetComplete(System.Windows.Controls.Button activeButton)
    {
        var state = (ButtonState)activeButton.Tag;

        // Stop spinning
        if (state.Icon.RenderTransform is RotateTransform rt)
            rt.BeginAnimation(RotateTransform.AngleProperty, null);

        // Swap to checkmark
        state.Icon.Symbol = SymbolRegular.Checkmark20;
        state.Icon.Foreground = ThemeBrushes.Success;
        state.Label.Text = "完成";
        state.Label.Foreground = ThemeBrushes.Success;
        _isLoading = false;
    }

    private void ResetButtons(System.Windows.Controls.Button activeButton)
    {
        _isLoading = false;
        var state = (ButtonState)activeButton.Tag;

        // Stop spinning
        if (state.Icon.RenderTransform is RotateTransform rt)
            rt.BeginAnimation(RotateTransform.AngleProperty, null);

        // Reset icon + label
        state.Icon.Symbol = state.OriginalIcon;
        state.Icon.Foreground = state.OriginalColor;
        state.Icon.RenderTransform = new ScaleTransform(1.0, 1.0);
        state.Label.Text = state.OriginalLabel;
        state.Label.Foreground = ThemeBrushes.TextSecondary;
        activeButton.Background = ThemeBrushes.Transparent;

        // Restore other buttons and separators
        foreach (var btn in _allButtons)
        {
            btn.IsHitTestVisible = true;
            btn.BeginAnimation(OpacityProperty, null);
            btn.Opacity = 1;
        }
        foreach (var sep in _allSeparators)
        {
            sep.BeginAnimation(OpacityProperty, null);
            sep.Opacity = 1;
        }
    }

    private static void StartSpinAnimation(SymbolIcon icon)
    {
        if (icon.RenderTransform is not RotateTransform rt)
        {
            rt = new RotateTransform(0);
            icon.RenderTransform = rt;
        }

        var spin = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1))
        {
            RepeatBehavior = RepeatBehavior.Forever
        };
        rt.BeginAnimation(RotateTransform.AngleProperty, spin);
    }

    #endregion

    #region Positioning

    private void PositionNearCapture()
    {
        UpdateLayout();
        if (Content is FrameworkElement content)
            content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var capturePixels = new Int32Rect(_capture.ScreenX, _capture.ScreenY, _capture.ScreenWidth, _capture.ScreenHeight);
        var screen = System.Windows.Forms.Screen.FromRectangle(new System.Drawing.Rectangle(
            capturePixels.X, capturePixels.Y,
            Math.Max(1, capturePixels.Width), Math.Max(1, capturePixels.Height))).WorkingArea;
        var workingArea = new Rect(screen.X, screen.Y, screen.Width, screen.Height);

        var width = ActualWidth > 0 ? ActualWidth : DesiredSize.Width;
        var height = ActualHeight > 0 ? ActualHeight : DesiredSize.Height;
        if (Content is FrameworkElement element)
        {
            width = Math.Max(width, element.DesiredSize.Width);
            height = Math.Max(height, element.DesiredSize.Height);
        }

        var dpiScale = GetDpiScaleForCapture(capturePixels);
        var menuPixels = CaptureMenuPlacementCalculator.ToPhysicalPixels(new Size(width, height), dpiScale.X, dpiScale.Y);
        var placement = CaptureMenuPlacementCalculator.Calculate(
            new Rect(capturePixels.X, capturePixels.Y, capturePixels.Width, capturePixels.Height),
            menuPixels, workingArea, CaptureGap, ScreenMargin, InsideMargin);

        PlaceWindowAtPixels(placement.Location);
    }

    private Point GetDpiScaleForCapture(Int32Rect capturePixels)
    {
        var rect = new NativeMethods.RECT
        {
            Left = capturePixels.X, Top = capturePixels.Y,
            Right = capturePixels.X + Math.Max(1, capturePixels.Width),
            Bottom = capturePixels.Y + Math.Max(1, capturePixels.Height)
        };
        var monitor = NativeMethods.MonitorFromRect(ref rect, NativeMethods.MonitorDefaultToNearest);
        if (monitor != IntPtr.Zero &&
            NativeMethods.GetDpiForMonitor(monitor, NativeMethods.MonitorDpiType.Effective, out var dpiX, out var dpiY) == 0)
            return new Point(dpiX / 96.0, dpiY / 96.0);

        var dpi = VisualTreeHelper.GetDpi(this);
        return new Point(dpi.DpiScaleX, dpi.DpiScaleY);
    }

    private void PlaceWindowAtPixels(Point location)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        _ = NativeMethods.SetWindowPos(handle, NativeMethods.HwndTopmost,
            (int)Math.Round(location.X), (int)Math.Round(location.Y),
            0, 0, NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
    }

    #endregion

    #region Actions

    private void RunAndClose(Action action)
    {
        try { action(); }
        catch (Exception ex)
        {
            _logService.Error(ex, "Capture menu action failed.");
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { CloseWithReview(); }
    }

    private async void RunLoadingAction(System.Windows.Controls.Button button, Func<Task> action)
    {
        var state = (ButtonState)button.Tag;
        SetLoading(button, $"{state.OriginalLabel}中...");

        try
        {
            await action();
            SetComplete(button);
            await Task.Delay(400);
            CloseWithReview();
        }
        catch (Exception ex)
        {
            _logService.Error(ex, "Capture menu action failed.");
            ResetButtons(button);
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnCopy() => _screenCaptureService.CopyToClipboard(_capture);

    private async Task OnTranslateAsync()
    {
        var result = await _translationService.TranslateAsync(new TranslationRequest
        {
            Capture = _capture,
            TargetLanguage = _settingsService.Current.DefaultTargetLanguage
        });
        var window = new TranslationWindow(_capture, result, _codexImportService, _settingsService);
        window.Show();
    }

    private async Task OnAskCodexAsync()
    {
        var outcome = await _codexImportService.ImportAsync(new CodexImportRequest
        {
            Capture = _capture,
            Options = _settingsService.Current.CodexImportOptions
        });
        ImportOutcomeDialog.ShowIfNeeded(outcome, this);
    }

    private void OnOpenFile()
    {
        Process.Start(new ProcessStartInfo(_capture.ImagePath) { UseShellExecute = true });
    }

    private void BringToFront()
    {
        Topmost = false;
        Topmost = true;
        Activate();
        Focus();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        e.Handled = true;
        CloseWithReview();
    }

    private void CloseWithReview()
    {
        if (_isLoading) return;
        SafeCloseReviewOverlay();
        _isClosing = true;
        Close();
    }

    private void SafeCloseReviewOverlay()
    {
        try { _closeReviewOverlay(); }
        catch (InvalidOperationException) { /* already closed */ }
    }

    public void CloseImmediately()
    {
        SafeCloseReviewOverlay();
        _isClosing = true;
        Close();
    }

    #endregion

    #region Animations

    private void AnimateIn()
    {
        var fade = new DoubleAnimation(0, 1, ThemeBrushes.Duration.Normal)
        {
            EasingFunction = ThemeBrushes.Easing.EntranceFade
        };
        BeginAnimation(OpacityProperty, fade);

        if (Content is Border border && border.RenderTransform is ScaleTransform st)
        {
            st.ScaleX = 0.85;
            st.ScaleY = 0.85;

            var bounce1 = new DoubleAnimation(0.85, 1.05, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = ThemeBrushes.Easing.EntranceBounce
            };
            bounce1.Completed += (_, _) =>
            {
                var bounce2 = new DoubleAnimation(1.05, 1.0, TimeSpan.FromMilliseconds(100))
                {
                    EasingFunction = ThemeBrushes.Easing.EntranceFade
                };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, bounce2);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, bounce2);
            };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, bounce1);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, bounce1);
        }
    }

    private void AnimateOut()
    {
        var fade = new DoubleAnimation(1, 0, ThemeBrushes.Duration.Exit)
        {
            EasingFunction = ThemeBrushes.Easing.ExitFade
        };
        fade.Completed += (_, _) =>
        {
            if (_isClosing) return;
            _isClosing = true;
            try { Close(); }
            catch (InvalidOperationException) { /* already closed via CloseImmediately */ }
        };
        BeginAnimation(OpacityProperty, fade);

        if (Content is Border border && border.RenderTransform is ScaleTransform st)
        {
            var shrink = new DoubleAnimation(1.0, 0.9, ThemeBrushes.Duration.Exit)
            {
                EasingFunction = ThemeBrushes.Easing.ExitFade
            };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
        }
    }

    private static void AnimateIconScale(ScaleTransform st, double to)
    {
        st.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(to, ThemeBrushes.Duration.Hover) { EasingFunction = ThemeBrushes.Easing.Hover });
        st.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(to, ThemeBrushes.Duration.Hover) { EasingFunction = ThemeBrushes.Easing.Hover });
    }

    #endregion

    private sealed class ButtonState
    {
        public SymbolIcon Icon { get; set; } = null!;
        public TextBlock Label { get; set; } = null!;
        public SymbolRegular OriginalIcon { get; set; }
        public string OriginalLabel { get; set; } = string.Empty;
        public Brush OriginalColor { get; set; } = Brushes.Black;
    }
}
