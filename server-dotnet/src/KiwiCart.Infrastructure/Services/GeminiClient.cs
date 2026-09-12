using System.Net;
using System.Text;
using System.Text.Json;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Exceptions;
using KiwiCart.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KiwiCart.Infrastructure.Services;

/// <summary>
/// Calls the Google Gemini generateContent REST endpoint directly via a named
/// HttpClient (same approach as the store clients), keeping the integration
/// fully controllable and free of third-party SDK dependencies.
/// </summary>
public class GeminiClient : IGeminiClient
{
    // The named HttpClient is configured in Program.cs with the Gemini base
    // address and its own Polly retry/timeout policies.
    public const string HttpClientName = "Gemini";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<GeminiOptions> options,
        ILogger<GeminiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateContentAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new GeminiApiException("Prompt must not be empty.");

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new GeminiApiException("Gemini API key is not configured.");

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                maxOutputTokens = _options.MaxOutputTokens,
                temperature = _options.Temperature
            }
        };

        var body = JsonSerializer.Serialize(payload);
        var relativeUrl =
            $"v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}";

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            using var response = await client.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                // Do not include the response body verbatim: it can echo the
                // API key in error messages. Log status only.
                _logger.LogError(
                    "Gemini request failed with status {StatusCode}", (int)response.StatusCode);
                throw new GeminiApiException(
                    $"Gemini API returned status {(int)response.StatusCode} ({response.StatusCode}).");
            }

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            var text = ExtractText(doc.RootElement);
            if (text is null)
            {
                _logger.LogWarning("Gemini response did not contain any candidate text.");
                throw new GeminiApiException("Gemini API response contained no text output.");
            }

            LogTokenUsage(doc.RootElement);

            return text.Trim();
        }
        catch (GeminiApiException)
        {
            // Already meaningful; rethrow without wrapping.
            throw;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Caller cancelled (e.g. client disconnected); let it propagate.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini request failed.");
            throw new GeminiApiException($"Gemini request failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract candidates[0].content.parts[*].text from a Gemini response,
    /// concatenating multiple parts. Returns null when no text is present.
    /// </summary>
    private static string? ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
            return null;

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array)
            return null;

        var sb = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textEl)
                && textEl.ValueKind == JsonValueKind.String)
            {
                sb.Append(textEl.GetString());
            }
        }

        return sb.Length == 0 ? null : sb.ToString();
    }

    /// <summary>
    /// Log token usage from the response's <c>usageMetadata</c> block for
    /// billing/quota visibility (plan requirement). Tolerant of a missing
    /// block or fields — older/edge responses may omit it, so absence is
    /// logged at debug rather than treated as an error.
    /// </summary>
    private void LogTokenUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usageMetadata", out var usage)
            || usage.ValueKind != JsonValueKind.Object)
        {
            _logger.LogDebug("Gemini response contained no usageMetadata; token usage unknown.");
            return;
        }

        var promptTokens = GetIntOrDefault(usage, "promptTokenCount");
        var candidateTokens = GetIntOrDefault(usage, "candidatesTokenCount");
        var totalTokens = GetIntOrDefault(usage, "totalTokenCount");

        _logger.LogInformation(
            "Gemini token usage: prompt={PromptTokens}, candidates={CandidateTokens}, total={TotalTokens}, model={Model}",
            promptTokens, candidateTokens, totalTokens, _options.Model);
    }

    private static int GetIntOrDefault(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var result)
            ? result
            : 0;
}
