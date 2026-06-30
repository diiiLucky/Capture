using System.Diagnostics;
using System.Windows;
using CodexCapture.Interop;
using CodexCapture.Models;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Forms = System.Windows.Forms;

namespace CodexCapture.Services;

public sealed class CodexImportService : IDisposable
{
    private const int SwRestore = 9;
    private readonly ScreenCaptureService _screenCaptureService;
    private readonly LogService _logService;
    private readonly UIA3Automation _automation = new();

    public CodexImportService(ScreenCaptureService screenCaptureService, LogService logService)
    {
        _screenCaptureService = screenCaptureService;
        _logService = logService;
    }

    public void Dispose()
    {
        _automation.Dispose();
    }

    public async Task<ImportOutcome> ImportAsync(CodexImportRequest request)
    {
        try
        {
            _screenCaptureService.CopyToClipboard(request.Capture);

            var process = await StartOrFindCodexAsync(request.Options).ConfigureAwait(false);
            if (process is null)
            {
                return await FallbackAsync(request, "StartCodex", "Could not start or find Codex.");
            }

            var handle = await WaitForMainWindowAsync(process, TimeSpan.FromSeconds(12));
            if (handle == IntPtr.Zero)
            {
                return await FallbackAsync(request, "FindWindow", "Codex did not expose a main window.");
            }

            NativeMethods.ShowWindow(handle, SwRestore);
            NativeMethods.SetForegroundWindow(handle);
            await Task.Delay(Math.Max(150, request.Options.PasteDelayMs));

            _logService.Info($"Codex import: sending new chat shortcut '{request.Options.NewChatShortcut}'.");
            Forms.SendKeys.SendWait(request.Options.NewChatShortcut);
            await Task.Delay(Math.Max(250, request.Options.PasteDelayMs));

            for (var attempt = 0; attempt < Math.Max(1, request.Options.PasteRetryCount); attempt++)
            {
                NativeMethods.SetForegroundWindow(handle);
                if (HasLikelyInputElement(handle))
                {
                    Forms.SendKeys.SendWait("^v");
                    await Task.Delay(500);
                    return new ImportOutcome { Success = true, Stage = "NewChatPasteImage" };
                }

                await Task.Delay(Math.Max(250, request.Options.PasteDelayMs));
            }

            return await FallbackAsync(request, "FindInput", "Codex 输入框未准备好，图片路径提示已复制到剪贴板。");
        }
        catch (Exception exception)
        {
            _logService.Error(exception, "Codex import failed.");
            return await FallbackAsync(request, "Exception", exception.Message);
        }
    }

    private async Task<Process?> StartOrFindCodexAsync(CodexImportOptions options)
    {
        var candidates = Process.GetProcesses()
            .Where(IsLikelyCodexDesktopProcess)
            .OrderByDescending(p => p.StartTimeSafe())
            .ToList();

        var running = candidates.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero) ?? candidates.FirstOrDefault();
        if (running is not null)
        {
            return running;
        }

        if (!string.IsNullOrWhiteSpace(options.CodexExePath) && File.Exists(options.CodexExePath))
        {
            var startInfo = new ProcessStartInfo(options.CodexExePath)
            {
                UseShellExecute = true,
                WorkingDirectory = Directory.Exists(options.PreferredWorkspacePath)
                    ? options.PreferredWorkspacePath
                    : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            return Process.Start(startInfo);
        }

        var packageLaunch = Process.Start(new ProcessStartInfo("explorer.exe", "shell:AppsFolder\\OpenAI.Codex_2p2nqsd0c76g0!App")
        {
            UseShellExecute = true
        });
        await Task.Delay(1500).ConfigureAwait(false);
        return Process.GetProcesses().Where(IsLikelyCodexDesktopProcess)
            .OrderByDescending(p => p.MainWindowHandle != IntPtr.Zero)
            .ThenByDescending(p => p.StartTimeSafe())
            .FirstOrDefault() ?? packageLaunch;
    }

    private bool IsLikelyCodexDesktopProcess(Process process)
    {
        if (process.Id == Environment.ProcessId)
        {
            return false;
        }

        if (process.ProcessName.Equals("CodexCapture", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!process.ProcessName.Equals("Codex", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            return string.IsNullOrWhiteSpace(process.MainModule?.FileName)
                   || process.MainModule.FileName.Contains("OpenAI.Codex", StringComparison.OrdinalIgnoreCase)
                   || process.MainModule.FileName.EndsWith("\\Codex.exe", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }

    private static async Task<IntPtr> WaitForMainWindowAsync(Process process, TimeSpan timeout)
    {
        var started = DateTimeOffset.Now;
        while (DateTimeOffset.Now - started < timeout)
        {
            process.Refresh();
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            await Task.Delay(250);
        }

        return IntPtr.Zero;
    }

    private bool HasLikelyInputElement(IntPtr handle)
    {
        try
        {
            var window = _automation.FromHandle(handle);
            return window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit)) is not null
                   || window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document)) is not null;
        }
        catch (Exception exception)
        {
            _logService.Error(exception, "FlaUI input detection failed.");
            return false;
        }
    }

    private async Task<ImportOutcome> FallbackAsync(CodexImportRequest request, string stage, string error)
    {
        var prompt = request.Options.OptionalPromptTemplate
            .Replace("{ImagePath}", request.Capture.ImagePath, StringComparison.OrdinalIgnoreCase)
            .Replace("{Width}", request.Capture.WidthPx.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{Height}", request.Capture.HeightPx.ToString(), StringComparison.OrdinalIgnoreCase);

        try
        {
            Clipboard.SetText(prompt);
            await Task.Delay(100);
            if (request.Options.FallbackMode == CodexFallbackMode.OpenFolderOnly)
            {
                var dir = Path.GetDirectoryName(request.Capture.ImagePath);
                if (dir is not null)
                {
                    Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
                }
            }

            return new ImportOutcome
            {
                Success = false,
                Stage = stage,
                UsedFallback = true,
                Error = error,
                RecoveryAction = "图片路径提示已复制到剪贴板，可直接粘贴到 Codex。"
            };
        }
        catch (Exception exception)
        {
            _logService.Error(exception, "Codex fallback failed.");
            return new ImportOutcome
            {
                Success = false,
                Stage = stage,
                UsedFallback = true,
                Error = $"{error}; fallback failed: {exception.Message}"
            };
        }
    }
}

internal static class ProcessExtensions
{
    public static DateTime StartTimeSafe(this Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
