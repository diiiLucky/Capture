using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CodexCapture.Models;
using CodexCapture.Theme;
using Wpf.Ui.Controls;

namespace CodexCapture;

public partial class MainWindow : FluentWindow
{
    private readonly string _historyDirectory;

    public MainWindow()
    {
        InitializeComponent();
        _historyDirectory = SettingsDefaults.HistoryDirectory;
        HistoryPathText.Text = TruncatePath(_historyDirectory, 36);
        ShortcutText.Text = "Ctrl + Alt + A";

        Loaded += (_, _) => StartStatusBreath();
    }

    public void SetStatus(bool ok, string message)
    {
        StatusDot.Fill = ok ? ThemeBrushes.Success : ThemeBrushes.Warning;
        StatusText.Text = message;
    }

    public void SetHotkeyText(string text)
    {
        ShortcutText.Text = text;
    }

    private void StartStatusBreath()
    {
        var breath = new DoubleAnimation(1.0, 0.35, ThemeBrushes.Duration.Breath)
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = ThemeBrushes.Easing.EntranceFade
        };
        StatusDot.BeginAnimation(OpacityProperty, breath);
    }

    private void OpenHistoryFolder(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo(_historyDirectory) { UseShellExecute = true });
    }

    private void OpenSettings(object sender, MouseButtonEventArgs e)
    {
        // Show settings via the app controller if available,
        // otherwise fallback: just open the settings file location
        try
        {
            var settingsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CodexCapture", "settings.json");
            if (System.IO.File.Exists(settingsPath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{settingsPath}\"") { UseShellExecute = false });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open settings location: {ex.Message}");
        }
    }

    private static string TruncatePath(string path, int maxLen)
    {
        if (path.Length <= maxLen) return path;
        var keep = maxLen - 3;
        if (keep <= 0) return path[..Math.Min(path.Length, maxLen)];
        return "..." + path[^keep..];
    }
}
