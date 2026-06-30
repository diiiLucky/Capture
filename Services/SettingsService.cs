using System.IO;
using System.Text.Json;
using CodexCapture.Models;

namespace CodexCapture.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private const string DefaultTargetLanguage = "\u4e2d\u6587";
    private readonly string _appDataDirectory;
    private readonly string _localDataDirectory;

    public SettingsService()
        : this(SettingsDefaults.AppDataDirectory, SettingsDefaults.LocalDataDirectory)
    {
    }

    public SettingsService(string appDataDirectory, string localDataDirectory)
    {
        _appDataDirectory = appDataDirectory;
        _localDataDirectory = localDataDirectory;
        SettingsPath = Path.Combine(_appDataDirectory, "settings.json");
    }

    public string SettingsPath { get; }

    public AppSettings Current { get; private set; } = AppSettings.CreateDefault();

    public async Task<AppSettings> LoadAsync()
    {
        Directory.CreateDirectory(_appDataDirectory);
        Directory.CreateDirectory(_localDataDirectory);
        Directory.CreateDirectory(GetUsableDirectory(Current.HistoryDirectory, SettingsDefaults.HistoryDirectory));

        if (!File.Exists(SettingsPath))
        {
            Current = Normalize(AppSettings.CreateDefault());
            await SaveAsync(Current);
            return Current;
        }

        try
        {
            await using var stream = File.OpenRead(SettingsPath);
            Current = Normalize(await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions)
                                ?? AppSettings.CreateDefault());
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or IOException)
        {
            BackupInvalidSettingsFile();
            Current = Normalize(AppSettings.CreateDefault());
            await SaveAsync(Current);
        }

        Directory.CreateDirectory(Current.HistoryDirectory);
        return Current;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Current = Normalize(settings);
        Directory.CreateDirectory(_appDataDirectory);
        Directory.CreateDirectory(_localDataDirectory);
        Directory.CreateDirectory(Current.HistoryDirectory);
        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, Current, JsonOptions);
    }

    public static AppSettings Normalize(AppSettings settings)
    {
        settings.GlobalHotkey ??= new HotkeySettings();
        settings.FloatingWidgetState ??= new FloatingWidgetState();
        settings.CodexImportOptions ??= new CodexImportOptions();
        settings.Providers ??= new List<ProviderProfile>();
        settings.HistoryDirectory = GetUsableDirectory(settings.HistoryDirectory, SettingsDefaults.HistoryDirectory);
        settings.DefaultTargetLanguage = string.IsNullOrWhiteSpace(settings.DefaultTargetLanguage)
            ? DefaultTargetLanguage
            : settings.DefaultTargetLanguage.Trim();

        if (settings.Providers.Count == 0)
        {
            settings.Providers.Add(ProviderProfile.CreateDefault());
        }

        foreach (var provider in settings.Providers)
        {
            var defaults = ProviderProfile.CreateDefault();
            provider.Id = string.IsNullOrWhiteSpace(provider.Id) ? defaults.Id : provider.Id;
            provider.DisplayName = string.IsNullOrWhiteSpace(provider.DisplayName) ? defaults.DisplayName : provider.DisplayName.Trim();
            provider.BaseUrl = string.IsNullOrWhiteSpace(provider.BaseUrl) ? defaults.BaseUrl : provider.BaseUrl.Trim();
            provider.Model = string.IsNullOrWhiteSpace(provider.Model) ? defaults.Model : provider.Model.Trim();
            provider.TimeoutSeconds = provider.TimeoutSeconds <= 0 ? defaults.TimeoutSeconds : provider.TimeoutSeconds;
            provider.CustomHeaders ??= new Dictionary<string, string>();
        }

        if (string.IsNullOrWhiteSpace(settings.SelectedProviderId) ||
            settings.Providers.All(provider => provider.Id != settings.SelectedProviderId))
        {
            settings.SelectedProviderId = settings.Providers[0].Id;
        }

        settings.CodexImportOptions.PreferredWorkspacePath =
            string.IsNullOrWhiteSpace(settings.CodexImportOptions.PreferredWorkspacePath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : settings.CodexImportOptions.PreferredWorkspacePath.Trim();
        settings.CodexImportOptions.NewChatShortcut =
            string.IsNullOrWhiteSpace(settings.CodexImportOptions.NewChatShortcut)
                ? "^n"
                : settings.CodexImportOptions.NewChatShortcut.Trim();
        settings.CodexImportOptions.PasteRetryCount = Math.Max(1, settings.CodexImportOptions.PasteRetryCount);
        settings.CodexImportOptions.PasteDelayMs = Math.Max(150, settings.CodexImportOptions.PasteDelayMs);

        return settings;
    }

    private static string GetUsableDirectory(string? value, string fallback)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return fallback;
        }

        try
        {
            var full = Path.GetFullPath(candidate);
            // Quick write-permission check: try to create a temp file.
            var testFile = Path.Combine(full, ".write_test");
            File.WriteAllText(testFile, string.Empty);
            File.Delete(testFile);
            return candidate;
        }
        catch
        {
            return fallback;
        }
    }

    private void BackupInvalidSettingsFile()
    {
        if (!File.Exists(SettingsPath))
        {
            return;
        }

        var backupPath = Path.Combine(
            _appDataDirectory,
            $"settings.invalid-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.json");
        try
        {
            File.Copy(SettingsPath, backupPath, overwrite: true);
        }
        catch
        {
            // A locked or unreadable settings file should not prevent recovery.
        }
    }
}
