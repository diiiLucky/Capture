using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CodexCapture.Models;
using CodexCapture.Services;
using CodexCapture.Theme;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;
using SymbolIcon = Wpf.Ui.Controls.SymbolIcon;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;
using TitleBar = Wpf.Ui.Controls.TitleBar;
using WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType;

namespace CodexCapture.Windows;

public sealed class TranslationWindow : FluentWindow
{
    private readonly CaptureResult _capture;
    private readonly CodexImportService _codexImportService;
    private readonly SettingsService _settingsService;
    private readonly TranslationResult _result;
    private readonly Action? _retryAction;

    private bool _isClosing;

    public TranslationWindow(
        CaptureResult capture,
        TranslationResult result,
        CodexImportService codexImportService,
        SettingsService settingsService,
        Action? retryAction = null)
    {
        _capture = capture;
        _result = result;
        _codexImportService = codexImportService;
        _settingsService = settingsService;
        _retryAction = retryAction;

        Title = "翻译结果";
        Width = 560;
        Height = 480;
        MinWidth = 380;
        MinHeight = 300;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowBackdropType = WindowBackdropType.Mica;
        ExtendsContentIntoTitleBar = true;
        Opacity = 0;

        Content = BuildContent();
        Loaded += (_, _) => AnimateEntrance();
        Closing += (_, e) =>
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                AnimateOut();
            }
        };
    }

    private UIElement BuildContent()
    {
        var grid = new Grid { Margin = new Thickness(24, 12, 24, 20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // TitleBar
        var titleBar = new TitleBar { Title = "翻译结果", Margin = new Thickness(0, 0, 0, 8) };
        Grid.SetRow(titleBar, 0);
        grid.Children.Add(titleBar);

        // Header row
        var header = BuildHeader();
        Grid.SetRow(header, 1);
        grid.Children.Add(header);

        // Body
        var body = _result.IsSuccess ? BuildSuccessBody() : BuildErrorBody();
        Grid.SetRow(body, 2);
        grid.Children.Add(body);

        // Footer buttons
        var footer = BuildFooter();
        Grid.SetRow(footer, 3);
        grid.Children.Add(footer);

        return grid;
    }

    private UIElement BuildHeader()
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

        // Title + badges row
        var badgeRow = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };

        // Source language pill
        badgeRow.Children.Add(LanguagePill(_result.DetectedLanguage ?? "原文"));
        badgeRow.Children.Add(new TextBlock
        {
            Text = " → ",
            FontSize = 13,
            Foreground = ThemeBrushes.TextTertiary,
            VerticalAlignment = VerticalAlignment.Center
        });
        // Target language pill
        badgeRow.Children.Add(LanguagePill(_settingsService.Current.DefaultTargetLanguage));

        // Dimension badge
        badgeRow.Children.Add(InfoBadge($"{_capture.WidthPx} × {_capture.HeightPx} px"));

        // Duration badge (only if successful)
        if (_result.IsSuccess && _result.Duration.TotalMilliseconds > 0)
        {
            badgeRow.Children.Add(InfoBadge($"{(int)_result.Duration.TotalSeconds}s"));
        }

        panel.Children.Add(badgeRow);

        // Divider
        panel.Children.Add(new Border
        {
            Height = 1,
            Background = ThemeBrushes.BorderSubtle,
            Margin = new Thickness(0, 10, 0, 0)
        });

        return panel;
    }

    private UIElement BuildSuccessBody()
    {
        if (string.IsNullOrWhiteSpace(_result.OriginalText) && string.IsNullOrWhiteSpace(_result.TranslatedText))
        {
            return new Border
            {
                Background = ThemeBrushes.Surface,
                BorderBrush = ThemeBrushes.BorderDefault,
                BorderThickness = new Thickness(1),
                CornerRadius = ThemeBrushes.Radius.MD,
                Padding = new Thickness(16),
                Child = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = ReadOnlyTextBox(_result.Text, ThemeBrushes.TextPrimary, 14)
                }
            };
        }

        // Two-section layout: original + translation
        var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var card = new Border
        {
            Background = ThemeBrushes.Surface,
            BorderBrush = ThemeBrushes.BorderDefault,
            BorderThickness = new Thickness(1),
            CornerRadius = ThemeBrushes.Radius.MD,
            Padding = new Thickness(16),
            Child = scrollViewer
        };

        var inner = new StackPanel();

        // Original section
        inner.Children.Add(SectionBlock("原文", _result.OriginalText));

        // Divider
        inner.Children.Add(new Border
        {
            Height = 1,
            Background = ThemeBrushes.BorderSubtle,
            Margin = new Thickness(0, 14, 0, 14)
        });

        // Translation section
        inner.Children.Add(SectionBlock("译文", _result.TranslatedText));

        scrollViewer.Content = inner;
        return card;
    }

    private static UIElement SectionBlock(string label, string? text)
    {
        var panel = new StackPanel();

        // Accent bar + label
        var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        headerRow.Children.Add(new Border
        {
            Width = 3,
            Background = ThemeBrushes.Primary,
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Stretch
        });
        headerRow.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = ThemeBrushes.TextTertiary
        });
        panel.Children.Add(headerRow);

        panel.Children.Add(ReadOnlyTextBox(
            string.IsNullOrWhiteSpace(text) ? "（空）" : text,
            ThemeBrushes.TextPrimary, 14));

        return panel;
    }

    private UIElement BuildErrorBody()
    {
        var card = new Border
        {
            Background = ThemeBrushes.Surface,
            BorderBrush = ThemeBrushes.BorderDefault,
            BorderThickness = new Thickness(1),
            CornerRadius = ThemeBrushes.Radius.MD,
            Padding = new Thickness(24),
            Child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new SymbolIcon
                    {
                        Symbol = SymbolRegular.Warning20,
                        FontSize = 32,
                        Foreground = ThemeBrushes.Error,
                        Margin = new Thickness(0, 0, 0, 12)
                    },
                    new TextBlock
                    {
                        Text = _result.Error ?? "翻译失败",
                        FontSize = 14,
                        Foreground = ThemeBrushes.TextPrimary,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 16)
                    }
                }
            }
        };

        // Add retry button if retry action provided
        if (_retryAction != null && card.Child is StackPanel sp)
        {
            var retryBtn = new Button
            {
                Content = "重试",
                MinWidth = 80,
                MinHeight = 36,
                Background = ThemeBrushes.Primary,
                Foreground = ThemeBrushes.White,
                BorderBrush = ThemeBrushes.Primary,
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            retryBtn.Click += (_, _) =>
            {
                _retryAction();
                _isClosing = true;
                Close();
            };
            sp.Children.Add(retryBtn);
        }

        return card;
    }

    private UIElement BuildFooter()
    {
        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        if (_result.IsSuccess)
        {
            var copyBtn = new Button
            {
                Content = "复制文本",
                MinWidth = 90,
                MinHeight = 36,
                Margin = new Thickness(0, 0, 8, 0),
                Background = ThemeBrushes.Surface,
                Foreground = ThemeBrushes.TextPrimary,
                BorderBrush = ThemeBrushes.BorderDefault,
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            copyBtn.Click += (_, _) =>
            {
                Clipboard.SetText(BuildClipboardText());
                copyBtn.Content = "已复制 ✓";
                copyBtn.Foreground = ThemeBrushes.Success;
            };

            var codexBtn = new Button
            {
                Content = "导入 Codex",
                MinWidth = 90,
                MinHeight = 36,
                Background = ThemeBrushes.Primary,
                Foreground = ThemeBrushes.White,
                BorderBrush = ThemeBrushes.Primary,
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            codexBtn.Click += async (_, _) =>
            {
                var outcome = await _codexImportService.ImportAsync(new CodexImportRequest
                {
                    Capture = _capture,
                    Options = _settingsService.Current.CodexImportOptions
                });
                var owner = IsLoaded ? this : Application.Current.MainWindow;
                ImportOutcomeDialog.ShowIfNeeded(outcome, owner);
            };

            bar.Children.Add(copyBtn);
            bar.Children.Add(codexBtn);
        }
        else
        {
            var closeBtn = new Button
            {
                Content = "关闭",
                MinWidth = 80,
                MinHeight = 36,
                Background = ThemeBrushes.Surface,
                Foreground = ThemeBrushes.TextPrimary,
                BorderBrush = ThemeBrushes.BorderDefault,
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeBtn.Click += (_, _) =>
            {
                _isClosing = true;
                Close();
            };
            bar.Children.Add(closeBtn);
        }

        return bar;
    }

    private string BuildClipboardText()
    {
        if (!_result.IsSuccess)
            return _result.Error ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_result.OriginalText) && string.IsNullOrWhiteSpace(_result.TranslatedText))
            return _result.Text ?? string.Empty;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_result.OriginalText))
            parts.Add($"原文：{_result.OriginalText}");
        if (!string.IsNullOrWhiteSpace(_result.TranslatedText))
            parts.Add($"译文：{_result.TranslatedText}");

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    #region Shared Helpers

    private static Border LanguagePill(string text) => new()
    {
        Background = ThemeBrushes.PrimarySubtle,
        CornerRadius = ThemeBrushes.Radius.Full,
        Padding = new Thickness(10, 4, 10, 4),
        Margin = new Thickness(0, 0, 6, 2),
        Child = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = ThemeBrushes.Primary
        }
    };

    private static Border InfoBadge(string text) => new()
    {
        Background = ThemeBrushes.SurfaceSecondary,
        CornerRadius = ThemeBrushes.Radius.Full,
        Padding = new Thickness(10, 4, 10, 4),
        Margin = new Thickness(0, 0, 6, 2),
        Child = new TextBlock
        {
            Text = text,
            FontSize = 11,
            Foreground = ThemeBrushes.TextTertiary
        }
    };

    private static TextBox ReadOnlyTextBox(string? text, Brush foreground, double fontSize) => new()
    {
        Text = text ?? string.Empty,
        TextWrapping = TextWrapping.Wrap,
        AcceptsReturn = true,
        IsReadOnly = true,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        BorderThickness = new Thickness(0),
        Background = ThemeBrushes.Transparent,
        Padding = new Thickness(0),
        FontSize = fontSize,
        Foreground = foreground
    };

    #endregion

    #region Animations

    private void AnimateEntrance()
    {
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = ThemeBrushes.Easing.EntranceFade,
            FillBehavior = FillBehavior.Stop
        };
        BeginAnimation(OpacityProperty, fade);
        Opacity = 1;

        if (Content is FrameworkElement fe)
        {
            fe.RenderTransformOrigin = new Point(0.5, 0.5);
            fe.RenderTransform = new ScaleTransform(0.92, 0.92);
            var scaleAnim = new DoubleAnimation(0.92, 1.0, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = ThemeBrushes.Easing.EntranceBounce
            };
            fe.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            fe.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
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
            _isClosing = true;
            Close();
        };
        BeginAnimation(OpacityProperty, fade);

        if (Content is FrameworkElement fe)
        {
            fe.RenderTransformOrigin = new Point(0.5, 0.5);
            fe.RenderTransform = new ScaleTransform(1.0, 1.0);
            var shrink = new DoubleAnimation(1.0, 0.95, ThemeBrushes.Duration.Exit)
            {
                EasingFunction = ThemeBrushes.Easing.ExitFade
            };
            fe.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
            fe.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
        }
    }

    #endregion
}
