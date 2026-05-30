using System.Text;
using System.Text.Json;

namespace PlanMate.Api.Services;

public readonly record struct GeminiMessage(string Role, string Content);

public sealed class GeminiCompletionService(HttpClient httpClient, IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] FallbackModels =
    [
        "gemini-2.5-flash",
        "gemini-2.0-flash-lite",
        "gemini-flash-latest"
    ];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GetApiKey());

    public async Task<string> CompleteAsync(
        string systemInstruction,
        IReadOnlyList<GeminiMessage> contents,
        double temperature = 0.7,
        int maxOutputTokens = 8192,
        int thinkingBudget = 0,
        CancellationToken cancellationToken = default)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Google API 키가 설정되지 않았습니다.");
        }

        var modelsToTry = BuildModelList();
        string? lastError = null;

        foreach (var model in modelsToTry)
        {
            var (success, text, error) = await TryGenerateAsync(
                apiKey, model, systemInstruction, contents, temperature, maxOutputTokens, thinkingBudget, cancellationToken);

            if (success)
            {
                return text;
            }

            lastError = error;
            if (!ShouldTryNextModel(error))
            {
                break;
            }
        }

        throw new InvalidOperationException(FormatUserError(lastError));
    }

    private IReadOnlyList<string> BuildModelList()
    {
        var primary = configuration["Ai:GeminiModel"] ?? "gemini-2.5-flash";
        return new[] { primary }
            .Concat(FallbackModels)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<(bool Success, string Text, string? Error)> TryGenerateAsync(
        string apiKey,
        string model,
        string systemInstruction,
        IReadOnlyList<GeminiMessage> contents,
        double temperature,
        int maxOutputTokens,
        int thinkingBudget,
        CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var generationConfig = new Dictionary<string, object>
        {
            ["temperature"] = temperature,
            ["maxOutputTokens"] = maxOutputTokens
        };

        // Gemini 2.5+ counts internal "thinking" tokens against maxOutputTokens.
        // Without a cap, simple chat replies can truncate mid-sentence (e.g. "안녕하세").
        if (SupportsThinkingConfig(model))
        {
            generationConfig["thinkingConfig"] = new { thinkingBudget };
        }

        var payload = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = contents.Select(m => new
            {
                role = MapRole(m.Role),
                parts = new[] { new { text = m.Content } }
            }),
            generationConfig = generationConfig
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return (false, "", $"[{model}] HTTP {(int)response.StatusCode}: {ExtractApiError(body)}");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return (false, "", $"[{model}] 응답에 candidates가 없습니다.");
            }

            var candidate = candidates[0];
            if (candidate.TryGetProperty("finishReason", out var finishReason)
                && finishReason.GetString() is "SAFETY" or "RECITATION")
            {
                return (false, "", $"[{model}] 안전 필터로 응답이 차단되었습니다.");
            }

            if (!candidate.TryGetProperty("content", out var content)
                || !content.TryGetProperty("parts", out var parts))
            {
                return (false, "", $"[{model}] 응답 형식이 올바르지 않습니다.");
            }

            var textBuilder = new StringBuilder();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textPart))
                {
                    var segment = textPart.GetString();
                    if (!string.IsNullOrEmpty(segment))
                    {
                        textBuilder.Append(segment);
                    }
                }
            }

            var text = textBuilder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, "", $"[{model}] 빈 응답이 반환되었습니다.");
            }

            return (true, text, null);
        }
        catch (Exception ex)
        {
            return (false, "", $"[{model}] 응답 파싱 실패: {ex.Message}");
        }
    }

    private static bool ShouldTryNextModel(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return true;
        }

        return error.Contains("HTTP 429", StringComparison.Ordinal)
               || error.Contains("HTTP 503", StringComparison.Ordinal)
               || error.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase)
               || error.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatUserError(string? lastError)
    {
        if (string.IsNullOrWhiteSpace(lastError))
        {
            return "Gemini API 호출에 실패했습니다.";
        }

        if (lastError.Contains("HTTP 429", StringComparison.Ordinal))
        {
            return "Gemini 무료 사용 한도를 초과했습니다. 1~2분 후 다시 시도하거나 Google AI Studio에서 할당량을 확인해 주세요.";
        }

        if (lastError.Contains("HTTP 403", StringComparison.Ordinal) || lastError.Contains("API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            if (lastError.Contains("leaked", StringComparison.OrdinalIgnoreCase))
            {
                return "Google API 키가 유출로 차단되었습니다. AI Studio에서 새 키를 발급한 뒤 appsettings.Development.json 또는 dotnet user-secrets에만 넣어 주세요.";
            }

            return "Google API 키가 올바르지 않거나 권한이 없습니다. AI Studio에서 새 키를 발급해 주세요.";
        }

        return $"Gemini 연결 실패: {lastError}";
    }

    private static string ExtractApiError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? body;
            }
        }
        catch
        {
            // ignore parse errors
        }

        return body.Length > 200 ? body[..200] + "..." : body;
    }

    private string? GetApiKey() =>
        configuration["Ai:GoogleApiKey"] ?? configuration["Ai:OpenAiApiKey"];

    private static string MapRole(string role) =>
        role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "model" : "user";

    private static bool SupportsThinkingConfig(string model) =>
        model.Contains("2.5", StringComparison.OrdinalIgnoreCase)
        || model.Contains("gemini-3", StringComparison.OrdinalIgnoreCase);
}
