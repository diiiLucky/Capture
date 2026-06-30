using System.Windows;
using CodexCapture.Models;

namespace CodexCapture.Windows;

internal static class ImportOutcomeDialog
{
    public static void ShowIfNeeded(ImportOutcome outcome, Window? owner = null)
    {
        if (outcome.Success)
        {
            return;
        }

        var message = string.IsNullOrWhiteSpace(outcome.RecoveryAction)
            ? outcome.Error ?? "导入 Codex 失败。"
            : $"{outcome.Error ?? "导入 Codex 失败。"}{Environment.NewLine}{outcome.RecoveryAction}";

        if (owner is null)
        {
            MessageBox.Show(message, "导入 Codex", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(owner, message, "导入 Codex", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
