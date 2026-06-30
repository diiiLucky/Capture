using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CodexCapture.Models;
using CodexCapture.Services;
using CodexCapture.Theme;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;
using TitleBar = Wpf.Ui.Controls.TitleBar;
using WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType;
using Forms = System.Windows.Forms;

namespace CodexCapture.Windows;

public sealed class SettingsWindow : FluentWindow
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly SecretService _secretService;
    private readonly AutoStartService _autoStartService;
    private readonly Action _restartHotkey;

    private readonly CheckBox _autoStartBox = new() { Content = "开机自启" };
    private readonly TextBox _historyDirectoryBox = new();
    private readonly TextBox _targetLanguageBox = new();
    private readonly TextBox _baseUrlBox = new();
    private readonly ComboBox _protocolBox = new();
    private readonly ComboBox _imagePayloadBox = new();
    private readonly TextBox _modelBox = new();
    private readonly PasswordBox _apiKeyBox = new();
    private readonly TextBox _codexPathBox = new();
    private readonly TextBox _workspacePathBox = new();
    private readonly TextBox _newChatShortcutBox = new();
    private readonly TextBlock _status = new() { Margin = new Thickness(0, 16, 0, 0), FontSize = 13 };

    public SettingsWindow(
        AppSettings settings,
        SettingsService settingsService,
        SecretService secretService,
        AutoStartService autoStartService,
        bool firstRun,
        Action restartHotkey)
    {
        _settings = settings;
        _settingsService = settingsService;
        _secretService = secretService;
        _autoStartService = autoStartService;
        _restartHotkey = restartHotkey;

        Title = "截图工具设置";
        Width = 640;
        Height = 800;
        MinWidth = 540;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = true;
        WindowBackdropType = WindowBackdropType.Mica;
        ExtendsContentIntoTitleBar = true;
        Opacity = 0;

        Content = BuildContent();
        LoadValues();
        Loaded += (_, _) =>
        {
            var anim = new DoubleAnimation(0, 1, ThemeBrushes.Duration.Normal)
            {
                EasingFunction = ThemeBrushes.Easing.EntranceFade
            };
            BeginAnimation(OpacityProperty, anim);
        };
    }

    private UIElement BuildContent()
    {
        var root = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel { Margin = new Thickness(28, 28, 28, 24) }
        };

        var panel = (StackPanel)root.Content;
        panel.Children.Add(new TitleBar { Title = "截图工具设置", Margin = new Thickness(0, 0, 0, 14) });

        // === Basic ===
        panel.Children.Add(SectionHeader("基础设置"));
        var basicSection = BeginSection(panel);
        basicSection.Children.Add(_autoStartBox);
        basicSection.Children.Add(FormLabel("截图历史目录"));
        basicSection.Children.Add(Row(_historyDirectoryBox, BrowseFolderButton(_historyDirectoryBox)));
        basicSection.Children.Add(FormHint("截图文件默认保存位置"));
        basicSection.Children.Add(FormLabel("默认翻译目标语言"));
        basicSection.Children.Add(_targetLanguageBox);
        basicSection.Children.Add(FormHint("例如：中文、English、日本語"));
        basicSection.Children.Add(FormLabel("悬浮组件"));
        basicSection.Children.Add(FormHint("点击截图，拖动移动。鼠标悬停时按钮会轻微放大"));

        // === AI Provider ===
        panel.Children.Add(SectionHeader("AI 供应商"));

        _protocolBox.ItemsSource = new[]
        {
            new OptionItem<AiProtocolMode>("Chat Completions", AiProtocolMode.ChatCompletions),
            new OptionItem<AiProtocolMode>("Responses", AiProtocolMode.Responses)
        };
        _protocolBox.DisplayMemberPath = nameof(OptionItem<AiProtocolMode>.Label);
        _imagePayloadBox.ItemsSource = new[]
        {
            new OptionItem<ImagePayloadMode>("Data URL（多数服务商）", ImagePayloadMode.DataUrl),
            new OptionItem<ImagePayloadMode>("纯 Base64", ImagePayloadMode.RawBase64)
        };
        _imagePayloadBox.DisplayMemberPath = nameof(OptionItem<ImagePayloadMode>.Label);

        var aiSection = BeginSection(panel);
        aiSection.Children.Add(FormLabel("协议"));
        aiSection.Children.Add(_protocolBox);
        aiSection.Children.Add(FormLabel("图片传输格式"));
        aiSection.Children.Add(_imagePayloadBox);
        aiSection.Children.Add(FormLabel("Base URL"));
        aiSection.Children.Add(_baseUrlBox);
        aiSection.Children.Add(FormHint("API 端点地址，例如 https://api.openai.com/v1"));
        aiSection.Children.Add(FormLabel("模型"));
        aiSection.Children.Add(_modelBox);
        aiSection.Children.Add(FormLabel("API Key"));
        aiSection.Children.Add(_apiKeyBox);

        // === Codex Import ===
        panel.Children.Add(SectionHeader("导入 Codex"));

        var codexSection = BeginSection(panel);
        codexSection.Children.Add(FormLabel("Codex 程序路径"));
        codexSection.Children.Add(Row(_codexPathBox, BrowseFileButton(_codexPathBox)));
        codexSection.Children.Add(FormLabel("默认工作目录"));
        codexSection.Children.Add(Row(_workspacePathBox, BrowseFolderButton(_workspacePathBox)));
        codexSection.Children.Add(FormLabel("新建对话快捷键"));
        codexSection.Children.Add(_newChatShortcutBox);
        codexSection.Children.Add(FormHint("默认 ^n（Ctrl+N）"));

        // === Actions ===
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 4, 0, 0)
        };
        var save = new Button
        {
            Content = "保存",
            MinWidth = 90,
            MinHeight = 36,
            Background = ThemeBrushes.Primary,
            Foreground = ThemeBrushes.White,
            BorderBrush = ThemeBrushes.Primary,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1.0, 1.0)
        };
        save.Click += async (_, _) => await SaveWithFeedbackAsync(save);
        var close = new Button
        {
            Content = "关闭",
            MinWidth = 90,
            MinHeight = 36,
            Margin = new Thickness(8, 0, 0, 0),
            Background = ThemeBrushes.Surface,
            Foreground = ThemeBrushes.TextPrimary,
            BorderBrush = ThemeBrushes.BorderDefault,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        close.Click += (_, _) => Close();
        actions.Children.Add(save);
        actions.Children.Add(close);
        panel.Children.Add(actions);
        panel.Children.Add(_status);
        return root;
    }

    private static StackPanel BeginSection(StackPanel parent)
    {
        var inner = new StackPanel();
        var grid = new Grid();

        // Accent bar at top
        var accentBar = new Border
        {
            Height = 3,
            Background = ThemeBrushes.Primary,
            CornerRadius = new CornerRadius(16, 16, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        var card = new Border
        {
            Background = ThemeBrushes.Surface,
            BorderBrush = ThemeBrushes.BorderDefault,
            BorderThickness = new Thickness(1),
            CornerRadius = ThemeBrushes.Radius.LG,
            Padding = new Thickness(20, 18, 20, 20),
            Margin = new Thickness(0, 0, 0, 20),
            Child = inner
        };

        grid.Children.Add(card);
        grid.Children.Add(accentBar);
        parent.Children.Add(grid);
        return inner;
    }

    private void LoadValues()
    {
        var provider = _settings.Providers.First();
        _autoStartBox.IsChecked = _settings.AutoStartEnabled;
        _historyDirectoryBox.Text = _settings.HistoryDirectory;
        _targetLanguageBox.Text = _settings.DefaultTargetLanguage;
        SelectOption(_protocolBox, provider.ProtocolMode);
        SelectOption(_imagePayloadBox, provider.ImagePayloadMode);
        _baseUrlBox.Text = provider.BaseUrl;
        _modelBox.Text = provider.Model;
        _codexPathBox.Text = _settings.CodexImportOptions.CodexExePath ?? string.Empty;
        _workspacePathBox.Text = _settings.CodexImportOptions.PreferredWorkspacePath;
        _newChatShortcutBox.Text = _settings.CodexImportOptions.NewChatShortcut;
    }

    private async Task SaveWithFeedbackAsync(Button saveButton)
    {
        await SaveAsync();

        // Press feedback
        if (saveButton.RenderTransform is ScaleTransform st)
        {
            var press = new DoubleAnimation(0.96, ThemeBrushes.Duration.Press)
            {
                EasingFunction = ThemeBrushes.Easing.Press,
                AutoReverse = true
            };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, press);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, press);
        }

        // Status fade in → auto fade out
        _status.Text = "已保存";
        _status.Foreground = ThemeBrushes.Success;
        _status.Opacity = 1;
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(800))
        {
            BeginTime = TimeSpan.FromSeconds(2),
            EasingFunction = ThemeBrushes.Easing.ExitFade
        };
        _status.BeginAnimation(OpacityProperty, fadeOut);
    }

    private async Task SaveAsync()
    {
        var provider = _settings.Providers.First();
        _settings.AutoStartEnabled = _autoStartBox.IsChecked == true;
        _settings.HistoryDirectory = string.IsNullOrWhiteSpace(_historyDirectoryBox.Text)
            ? SettingsDefaults.HistoryDirectory
            : _historyDirectoryBox.Text.Trim();
        _settings.DefaultTargetLanguage = string.IsNullOrWhiteSpace(_targetLanguageBox.Text)
            ? "中文"
            : _targetLanguageBox.Text.Trim();
        provider.ProtocolMode = SelectedValue(_protocolBox, AiProtocolMode.ChatCompletions);
        provider.ImagePayloadMode = SelectedValue(_imagePayloadBox, ImagePayloadMode.DataUrl);
        var defaultProvider = ProviderProfile.CreateDefault();
        provider.BaseUrl = string.IsNullOrWhiteSpace(_baseUrlBox.Text)
            ? defaultProvider.BaseUrl
            : _baseUrlBox.Text.Trim();
        provider.Model = string.IsNullOrWhiteSpace(_modelBox.Text)
            ? defaultProvider.Model
            : _modelBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(_apiKeyBox.Password))
            provider.EncryptedApiKey = _secretService.Protect(_apiKeyBox.Password);

        _settings.CodexImportOptions.CodexExePath = string.IsNullOrWhiteSpace(_codexPathBox.Text)
            ? null : _codexPathBox.Text.Trim();
        _settings.CodexImportOptions.PreferredWorkspacePath = string.IsNullOrWhiteSpace(_workspacePathBox.Text)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : _workspacePathBox.Text.Trim();
        _settings.CodexImportOptions.NewChatShortcut = string.IsNullOrWhiteSpace(_newChatShortcutBox.Text)
            ? "^n" : _newChatShortcutBox.Text.Trim();

        try
        {
            _autoStartService.SetEnabled(_settings.AutoStartEnabled);
            await _settingsService.SaveAsync(_settings);
            LoadValues();
            _restartHotkey();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    #region UI Helpers

    private static TextBlock SectionHeader(string text) => new()
    {
        Text = $"●  {text}",
        FontSize = 16,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 28, 0, 12),
        Foreground = ThemeBrushes.TextPrimary
    };

    private static TextBlock FormLabel(string text) => new()
    {
        Text = text,
        Margin = new Thickness(0, 10, 0, 6),
        FontSize = 13,
        Foreground = ThemeBrushes.TextSecondary
    };

    private static TextBlock FormHint(string text) => new()
    {
        Text = text,
        Margin = new Thickness(0, 4, 0, 2),
        FontSize = 12,
        Foreground = ThemeBrushes.TextTertiary
    };

    private static UIElement Row(TextBox textBox, Button button)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(textBox, 0);
        Grid.SetColumn(button, 1);
        textBox.Margin = new Thickness(0, 0, 8, 0);
        grid.Children.Add(textBox);
        grid.Children.Add(button);
        return grid;
    }

    private static Button BrowseFolderButton(TextBox target)
    {
        var btn = new Button
        {
            Content = "浏览",
            MinWidth = 64,
            MinHeight = 36,
            FontSize = 12,
            Background = ThemeBrushes.SurfaceSecondary,
            BorderBrush = ThemeBrushes.BorderDefault,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btn.Click += (_, _) =>
        {
            using var dialog = new Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
                target.Text = dialog.SelectedPath;
        };
        return btn;
    }

    private static Button BrowseFileButton(TextBox target)
    {
        var btn = new Button
        {
            Content = "浏览",
            MinWidth = 64,
            MinHeight = 36,
            FontSize = 12,
            Background = ThemeBrushes.SurfaceSecondary,
            BorderBrush = ThemeBrushes.BorderDefault,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btn.Click += (_, _) =>
        {
            using var dialog = new Forms.OpenFileDialog { Filter = "Executable|*.exe|All files|*.*" };
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
                target.Text = dialog.FileName;
        };
        return btn;
    }

    private static void SelectOption<T>(ComboBox comboBox, T value)
    {
        foreach (var item in comboBox.Items.OfType<OptionItem<T>>())
        {
            if (EqualityComparer<T>.Default.Equals(item.Value, value))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private static T SelectedValue<T>(ComboBox comboBox, T fallback) =>
        comboBox.SelectedItem is OptionItem<T> item ? item.Value : fallback;

    private sealed record OptionItem<T>(string Label, T Value);

    #endregion
}
