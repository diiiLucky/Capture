using System.Net.Http;
using System.Text;
using System.Text.Json;
using CodexCapture.Models;

namespace CodexCapture.Services;

public sealed class AiRequestFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public PreparedAiRequest CreateTranslationRequest(
        ProviderProfile provider,
        string apiKey,
        string imagePath,
        string targetLanguage)
    {
        var endpoint = provider.ProtocolMode == AiProtocolMode.Responses
            ? CombineUrl(provider.BaseUrl, "responses")
            : CombineUrl(provider.BaseUrl, "chat/completions");

        var prompt =
            $"请识别截图里的全部可读文字，判断原文语言，并翻译成{targetLanguage}。" +
            "请只返回 JSON，不要使用 Markdown 代码块。JSON 格式必须为：" +
            "{\"detectedLanguage\":\"原文语言\",\"originalText\":\"识别出的原文\",\"translatedText\":\"译文\"}。";

        var imageBytes = File.ReadAllBytes(imagePath);
        var base64 = Convert.ToBase64String(imageBytes);
        var imageValue = provider.ImagePayloadMode == ImagePayloadMode.RawBase64
            ? base64
            : "data:image/png;base64," + base64;
        var payload = provider.ProtocolMode == AiProtocolMode.Responses
            ? CreateResponsesPayload(provider.Model, prompt, imageValue)
            : CreateChatCompletionsPayload(provider.Model, prompt, imageValue);

        var json = JsonSerializer.Serialize(payload, JsonOptions);

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        foreach (var header in provider.CustomHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return new PreparedAiRequest(
            request,
            endpoint,
            provider.ProtocolMode,
            provider.ImagePayloadMode,
            imageBytes.Length,
            json.Length);
    }

    private static object CreateResponsesPayload(string model, string prompt, string imageValue) => new
    {
        model,
        input = new object[]
        {
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "input_text", text = prompt },
                    new { type = "input_image", image_url = imageValue }
                }
            }
        }
    };

    private static object CreateChatCompletionsPayload(string model, string prompt, string imageValue) => new
    {
        model,
        messages = new object[]
        {
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = prompt },
                    new { type = "image_url", image_url = new { url = imageValue } }
                }
            }
        }
    };

    private static string CombineUrl(string baseUrl, string endpoint)
    {
        var trimmed = baseUrl.TrimEnd('/');
        if (trimmed.EndsWith("/responses", StringComparison.OrdinalIgnoreCase) ||
            trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return $"{trimmed}/{endpoint}";
    }

    public static string? ExtractText(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("output_text", out var outputText))
        {
            return outputText.GetString();
        }

        if (document.RootElement.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return ExtractContentText(content);
            }
        }

        if (document.RootElement.TryGetProperty("output", out var output))
        {
            return ExtractContentText(output);
        }

        return null;
    }

    public static TranslationResult ExtractTranslationResult(string json)
    {
        var text = ExtractText(json) ?? json;
        if (TryParseTranslationJson(text, out var result))
        {
            return new TranslationResult
            {
                DetectedLanguage = result.DetectedLanguage,
                OriginalText = result.OriginalText,
                TranslatedText = result.TranslatedText,
                Text = BuildCombinedText(result)
            };
        }

        return new TranslationResult { Text = text };
    }

    private static bool TryParseTranslationJson(string text, out TranslationResult result)
    {
        result = new TranslationResult();
        try
        {
            using var document = JsonDocument.Parse(StripCodeFence(text.Trim()));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var originalText = GetString(root, "originalText", "original_text", "sourceText", "source_text", "original");
            var translatedText = GetString(root, "translatedText", "translated_text", "translation", "translated");
            if (string.IsNullOrWhiteSpace(originalText) && string.IsNullOrWhiteSpace(translatedText))
            {
                return false;
            }

            result = new TranslationResult
            {
                DetectedLanguage = GetString(root, "detectedLanguage", "detected_language", "sourceLanguage", "source_language"),
                OriginalText = originalText,
                TranslatedText = translatedText
            };
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                return ExtractContentText(value);
            }
        }

        return null;
    }

    private static string StripCodeFence(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal))
        {
            return text;
        }

        var firstLineEnd = text.IndexOf('\n');
        var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
        return firstLineEnd >= 0 && lastFence > firstLineEnd
            ? text[(firstLineEnd + 1)..lastFence].Trim()
            : text;
    }

    private static string? BuildCombinedText(TranslationResult result)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(result.DetectedLanguage))
        {
            parts.Add($"原文语言：{result.DetectedLanguage}");
        }

        if (!string.IsNullOrWhiteSpace(result.OriginalText))
        {
            parts.Add($"原文：{result.OriginalText}");
        }

        if (!string.IsNullOrWhiteSpace(result.TranslatedText))
        {
            parts.Add($"译文：{result.TranslatedText}");
        }

        return parts.Count > 0 ? string.Join(Environment.NewLine + Environment.NewLine, parts) : null;
    }

    private static string? ExtractContentText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var parts = new List<string>();
            foreach (var item in element.EnumerateArray())
            {
                var text = ExtractContentText(item);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    parts.Add(text);
                }
            }

            return parts.Count > 0 ? string.Join(Environment.NewLine, parts) : null;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("text", out var text))
            {
                return ExtractContentText(text);
            }

            if (element.TryGetProperty("content", out var content))
            {
                return ExtractContentText(content);
            }
        }

        return null;
    }
}

public sealed record PreparedAiRequest(
    HttpRequestMessage Request,
    string Endpoint,
    AiProtocolMode ProtocolMode,
    ImagePayloadMode ImagePayloadMode,
    long ImageBytes,
    int JsonBytes) : IDisposable
{
    public void Dispose() => Request.Dispose();
}
