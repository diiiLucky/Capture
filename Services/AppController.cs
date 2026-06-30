using System.Drawing;
using System.Windows;
using CodexCapture.Models;
using CodexCapture.Windows;
using Forms = System.Windows.Forms;

namespace CodexCapture.Services;

public sealed class AppController : IAsyncDisposable
{
    private readonly SettingsService _settingsService = new();
    private readonly SecretService _secretService = new();
    private readonly LogService _logService = new();
    private readonly AutoStartService _autoStartService = new();
    private readonly HotkeyService _hotkeyService = new();
    private readonly WindowDetectionService _windowDetectionService = new();
    private readonly ScreenCaptureService _screenCaptureService = new();
    private readonly AiRequestFactory _aiRequestFactory = new();

    private AiTranslationService? _translationService;
    private CodexImportService? _codexImportService;
    private Forms.NotifyIcon? _notifyIcon;
    private FloatingWidgetWindow? _floatingWidget;
    private HotkeySinkWindow? _hotkeySink;
    private CaptureOverlayWindow? _currentCaptureOverlay;
    private CaptureReviewOverlayWindow? _currentReviewOverlay;
    private CaptureMenuWindow? _currentCaptureMenu;
    private AppSettings _settings = AppSettings.CreateDefault();

    public async Task StartAsync(bool minimized)
    {
        _settings = await _settingsService.LoadAsync();
        _settings.AutoStartEnabled = _autoStartService.IsEnabled();
        _translationService = new AiTranslationService(_settingsService, _secretService, _aiRequestFactory, _logService);
        _codexImportService = new CodexImportService(_screenCaptureService, _logService);

        CreateTrayIcon();
        CreateHotkeySink();
        ShowFloatingWidgetIfNeeded();

        if (!minimized && NeedsFirstRun())
        {
            ShowSettingsWindow(owner: null, firstRun: true);
        }
    }

    public async ValueTask DisposeAsync()
    {
        CancelCurrentCaptureSession();
        _hotkeyService.Dispose();

        if (_hotkeySink is not null)
        {
            _hotkeySink.Close();
            _hotkeySink = null;
        }

        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _codexImportService?.Dispose();
        if (_floatingWidget is not null)
        {
            SaveFloatingPosition();
            _floatingWidget.Close();
            _floatingWidget = null;
        }

        await _settingsService.SaveAsync(_settings);
    }

    public void BeginCapture()
    {
        CancelCurrentCaptureSession();
        var overlay = new CaptureOverlayWindow(_windowDetectionService, _screenCaptureService, _settings.HistoryDirectory);
        _currentCaptureOverlay = overlay;
        overlay.CaptureCompleted += (_, capture) => ShowCaptureMenu(capture);
        overlay.Closed += (_, _) =>
        {
            if (ReferenceEquals(_currentCaptureOverlay, overlay))
            {
                _currentCaptureOverlay = null;
            }
        };
        overlay.Show();
        overlay.Activate();
    }

    private void ShowCaptureMenu(CaptureResult capture)
    {
        CloseWindow(_currentCaptureMenu);
        CloseWindow(_currentReviewOverlay);

        var reviewOverlay = new CaptureReviewOverlayWindow(capture);
        _currentReviewOverlay = reviewOverlay;
        reviewOverlay.Closed += (_, _) =>
        {
            if (ReferenceEquals(_currentReviewOverlay, reviewOverlay))
            {
                _currentReviewOverlay = null;
            }
        };
        reviewOverlay.Show();

        if (_translationService is null || _codexImportService is null)
        {
            _logService.Error("Translation or Codex import service is not initialized.");
            return;
        }

        var menu = new CaptureMenuWindow(
            capture,
            _screenCaptureService,
            _translationService,
            _codexImportService,
            _settingsService,
            _logService,
            reviewOverlay.Close);
        _currentCaptureMenu = menu;
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_currentCaptureMenu, menu))
            {
                _currentCaptureMenu = null;
            }
        };
        menu.Show();
        menu.Activate();
    }

    private void CancelCurrentCaptureSession()
    {
        CloseWindow(_currentCaptureMenu);
        CloseWindow(_currentReviewOverlay);
        CloseWindow(_currentCaptureOverlay);
        _currentCaptureMenu = null;
        _currentReviewOverlay = null;
        _currentCaptureOverlay = null;
    }

    private static void CloseWindow(Window? window)
    {
        if (window is null)
        {
            return;
        }

        try
        {
            if (window is CaptureMenuWindow menu)
            {
                menu.CloseImmediately();
                return;
            }

            window.Close();
        }
        catch (InvalidOperationException)
        {
            // Window is already closing or closed.
        }
    }

    private bool NeedsFirstRun()
    {
        var selected = _settings.Providers.FirstOrDefault(p => p.Id == _settings.SelectedProviderId);
        return selected is null || string.IsNullOrWhiteSpace(selected.EncryptedApiKey);
    }

    private void CreateTrayIcon()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "截图工具",
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };

        _notifyIcon.ContextMenuStrip.Items.Add("开始截图", null, (_, _) => BeginCapture());
        _notifyIcon.ContextMenuStrip.Items.Add("显示/隐藏悬浮窗", null, async (_, _) => await ToggleFloatingWidgetAsync());
        _notifyIcon.ContextMenuStrip.Items.Add("设置", null, (_, _) => ShowSettingsWindow(null, false));
        _notifyIcon.ContextMenuStrip.Items.Add("退出", null, async (_, _) =>
        {
            await DisposeAsync();
            Application.Current.Shutdown();
        });
        _notifyIcon.DoubleClick += (_, _) => BeginCapture();
    }

    private void CreateHotkeySink()
    {
        _hotkeySink = new HotkeySinkWindow();
        _hotkeySink.Show();
        _hotkeySink.Hide();
        _hotkeyService.Pressed += (_, _) => BeginCapture();
        if (!_hotkeyService.Register(_hotkeySink, _settings.GlobalHotkey))
        {
            _logService.Error($"Failed to register hotkey: {_settings.GlobalHotkey}");
        }
    }

    private void ShowFloatingWidgetIfNeeded()
    {
        if (!_settings.FloatingWidgetState.IsVisible)
        {
            return;
        }

        _floatingWidget = new FloatingWidgetWindow(this, _settings);
        _floatingWidget.Show();
    }

    private async Task ToggleFloatingWidgetAsync()
    {
        if (_floatingWidget is null)
        {
            _settings.FloatingWidgetState.IsVisible = true;
            ShowFloatingWidgetIfNeeded();
        }
        else
        {
            SaveFloatingPosition();
            _settings.FloatingWidgetState.IsVisible = false;
            _floatingWidget.Close();
            _floatingWidget = null;
        }

        await _settingsService.SaveAsync(_settings);
    }

    private void SaveFloatingPosition()
    {
        if (_floatingWidget is null)
        {
            return;
        }

        _settings.FloatingWidgetState.Left = _floatingWidget.Left;
        _settings.FloatingWidgetState.Top = _floatingWidget.Top;
    }

    private void ShowSettingsWindow(Window? owner, bool firstRun)
    {
        var window = new SettingsWindow(
            _settings,
            _settingsService,
            _secretService,
            _autoStartService,
            firstRun,
            RestartHotkey);
        if (owner is not null)
        {
            window.Owner = owner;
        }

        window.Show();
        window.Activate();
    }

    private void RestartHotkey()
    {
        if (_hotkeySink is not null)
        {
            if (!_hotkeyService.Register(_hotkeySink, _settings.GlobalHotkey))
            {
                _logService.Error($"Failed to re-register hotkey after settings change: {_settings.GlobalHotkey}");
            }
        }
    }
}
