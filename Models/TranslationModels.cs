namespace CodexCapture.Models;

public sealed class TranslationRequest
{
    public required CaptureResult Capture { get; init; }

    public required string TargetLanguage { get; init; }
}

public sealed class TranslationResult
{
    public string? DetectedLanguage { get; init; }

    public string? OriginalText { get; init; }

    public string? TranslatedText { get; init; }

    public string? Text { get; init; }

    public string? ProviderName { get; init; }

    public TimeSpan Duration { get; init; }

    public string? Error { get; init; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(Error);
}
