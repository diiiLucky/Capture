using Microsoft.Win32;

namespace CodexCapture.Services;

public sealed class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CodexCapture";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(ValueName) is string value && value.Contains(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
                       ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, true);

        if (enabled)
        {
            var exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "CodexCapture.exe");
            key.SetValue(ValueName, $"\"{exe}\" --minimized");
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
    }
}
