namespace CodexCapture.Models;

public sealed class ProviderProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string DisplayName { get; set; } = "OpenAI Compatible";

    public AiProtocolMode ProtocolMode { get; set; } = AiProtocolMode.ChatCompletions;

    public ImagePayloadMode ImagePayloadMode { get; set; } = ImagePayloadMode.DataUrl;

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public string Model { get; set; } = "gpt-4o-mini";

    public string? EncryptedApiKey { get; set; }

    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    public int TimeoutSeconds { get; set; } = 120;

    public static ProviderProfile CreateDefault() => new();
}

public enum AiProtocolMode
{
    Responses,
    ChatCompletions
}

public enum ImagePayloadMode
{
    DataUrl,
    RawBase64
}
