namespace CodexCapture.Models;

public sealed class AppSettings
{
    public bool AutoStartEnabled { get; set; }

    public HotkeySettings GlobalHotkey { get; set; } = new();

    public FloatingWidgetState FloatingWidgetState { get; set; } = new();

    public string HistoryDirectory { get; set; } = SettingsDefaults.HistoryDirectory;

    public string DefaultTargetLanguage { get; set; } = "中文";

    public List<ProviderProfile> Providers { get; set; } = new();

    public string? SelectedProviderId { get; set; }

    public CodexImportOptions CodexImportOptions { get; set; } = new();

    public static AppSettings CreateDefault()
    {
        var provider = ProviderProfile.CreateDefault();
        return new AppSettings
        {
            Providers = [provider],
            SelectedProviderId = provider.Id
        };
    }
}

public static class SettingsDefaults
{
    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CodexCapture");

    public static string LocalDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexCapture");

    public static string HistoryDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "CodexCapture");
}
