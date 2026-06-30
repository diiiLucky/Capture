using System.Diagnostics;
using CodexCapture.Models;

namespace CodexCapture.Services;

public sealed class AiTranslationService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(120)
    };

    private readonly SettingsService _settingsService;
    private readonly SecretService _secretService;
    private readonly AiRequestFactory _requestFactory;
    private readonly LogService _logService;

    public AiTranslationService(
        SettingsService settingsService,
        SecretService secretService,
        AiRequestFactory requestFactory,
        LogService logService)
    {
        _settingsService = settingsService;
        _secretService = secretService;
        _requestFactory = requestFactory;
        _logService = logService;
    }

    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.Current;
        var provider = settings.Providers.FirstOrDefault(p => p.Id == settings.SelectedProviderId)
                       ?? settings.Providers.FirstOrDefault();

        if (provider is null)
        {
            return new TranslationResult { Error = "未配置 AI 供应商。" };
        }

        var apiKey = _secretService.Unprotect(provider.EncryptedApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new TranslationResult { ProviderName = provider.DisplayName, Error = "API Key 为空。" };
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var preparedRequest = _requestFactory.CreateTranslationRequest(
                provider,
                apiKey,
                request.Capture.ImagePath,
                request.TargetLanguage);
            using var httpRequest = preparedRequest.Request;

            _logService.Info(
                $"AI request: endpoint={preparedRequest.Endpoint}, protocol={preparedRequest.ProtocolMode}, " +
                $"imagePayload={preparedRequest.ImagePayloadMode}, imageBytes={preparedRequest.ImageBytes}, jsonBytes={preparedRequest.JsonBytes}");

            using var response = await HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            _logService.Info($"AI response: status={(int)response.StatusCode}, bodyPreview={Preview(body)}");

            if (!response.IsSuccessStatusCode)
            {
                _logService.Error($"AI request failed: {(int)response.StatusCode} {body}");
                return new TranslationResult
                {
                    ProviderName = provider.DisplayName,
                    Duration = stopwatch.Elapsed,
                    Error = $"AI 请求失败：{(int)response.StatusCode} {response.ReasonPhrase}。详情已写入日志。"
                };
            }

            var result = AiRequestFactory.ExtractTranslationResult(body);
            return new TranslationResult
            {
                ProviderName = provider.DisplayName,
                Duration = stopwatch.Elapsed,
                DetectedLanguage = result.DetectedLanguage,
                OriginalText = result.OriginalText,
                TranslatedText = result.TranslatedText,
                Text = result.Text
            };
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logService.Error(exception, "AI translation failed.");
            return new TranslationResult
            {
                ProviderName = provider.DisplayName,
                Duration = stopwatch.Elapsed,
                Error = $"翻译失败：{exception.Message}"
            };
        }
    }

    private static string Preview(string text)
    {
        var compact = text.Replace("\r", " ").Replace("\n", " ");
        return compact.Length <= 500 ? compact : compact[..500];
    }
}
