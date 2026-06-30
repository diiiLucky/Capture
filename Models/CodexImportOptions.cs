namespace CodexCapture.Models;

public sealed class CodexImportOptions
{
    public string? CodexExePath { get; set; }

    public string PreferredWorkspacePath { get; set; } =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public string NewChatShortcut { get; set; } = "^n";

    public int PasteRetryCount { get; set; } = 2;

    public int PasteDelayMs { get; set; } = 900;

    public CodexFallbackMode FallbackMode { get; set; } = CodexFallbackMode.ImagePathPrompt;

    public string OptionalPromptTemplate { get; set; } =
        "请帮我分析这张截图。图片路径：{ImagePath}";
}

public enum CodexFallbackMode
{
    ImagePathPrompt,
    OpenFolderOnly
}
