namespace CodexCapture.Models;

public sealed class CodexImportRequest
{
    public required CaptureResult Capture { get; init; }

    public required CodexImportOptions Options { get; init; }
}

public sealed class ImportOutcome
{
    public bool Success { get; init; }

    public string Stage { get; init; } = "NotStarted";

    public bool UsedFallback { get; init; }

    public string? Error { get; init; }

    public string? RecoveryAction { get; init; }
}
