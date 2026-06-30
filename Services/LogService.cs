using System.IO;
using CodexCapture.Models;

namespace CodexCapture.Services;

public sealed class LogService
{
    private readonly string _logPath = Path.Combine(SettingsDefaults.LocalDataDirectory, "codex-capture.log");
    private readonly object _gate = new();

    public void Info(string message) => Write("INFO", message);

    public void Error(Exception exception, string message) =>
        Write("ERROR", $"{message}{Environment.NewLine}{exception}");

    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        var line = $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}";
        lock (_gate)
        {
            File.AppendAllText(_logPath, line);
        }
    }
}
