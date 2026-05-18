using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StealthPrompt;

public sealed class OpenAiClient
{
    private static readonly Uri ResponsesEndpoint = new("https://api.openai.com/v1/responses");
    private static readonly Uri GroqChatEndpoint = new("https://api.groq.com/openai/v1/chat/completions");
    private readonly HttpClient _httpClient = new();

    public async Task<string> SendAsync(AppSettings settings, string selectedText, CancellationToken cancellationToken, string? extraContext = null)
    {
        if (settings.Provider.Equals("groq", StringComparison.OrdinalIgnoreCase))
        {
            return await SendGroqAsync(settings, selectedText, cancellationToken, extraContext);
        }

        return await SendOpenAiAsync(settings, selectedText, cancellationToken, extraContext);
    }

    private async Task<string> SendOpenAiAsync(AppSettings settings, string selectedText, CancellationToken cancellationToken, string? extraContext)
    {
        var apiKey = CredentialStore.LoadApiKey("openai");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Missing OpenAI API key. Add it in Settings or set OPENAI_API_KEY.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Math.Max(5000, settings.TimeoutMs));

        var prompt = BuildPrompt(settings, selectedText, extraContext);
        var payload = new
        {
            model = settings.Model,
            instructions = DefaultSystemPrompt,
            input = prompt
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ResponsesEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(ExtractError(body) ?? $"OpenAI request failed: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        return ExtractText(body) ?? throw new InvalidOperationException("OpenAI response did not contain text.");
    }

    private async Task<string> SendGroqAsync(AppSettings settings, string selectedText, CancellationToken cancellationToken, string? extraContext)
    {
        var apiKey = CredentialStore.LoadApiKey("groq");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Missing Groq API key. Add it in Settings or set GROQ_API_KEY.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Math.Max(5000, settings.TimeoutMs));

        var payload = new
        {
            model = settings.Model,
            messages = new[]
            {
                new { role = "system", content = DefaultSystemPrompt },
                new { role = "user", content = BuildPrompt(settings, selectedText, extraContext) }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, GroqChatEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(ExtractError(body) ?? $"Groq request failed: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        return ExtractChatText(body) ?? throw new InvalidOperationException("Groq response did not contain text.");
    }

    public static string BuildPrompt(AppSettings settings, string selectedText, string? extraContext = null)
    {
        var prompt = "Process this selected text:\r\n\r\n" + selectedText;
        if (string.IsNullOrWhiteSpace(extraContext))
        {
            return prompt;
        }

        return "Use this HRDB context/instructions when answering:\r\n\r\n" +
               extraContext +
               "\r\n\r\nUser selected text:\r\n\r\n" +
               prompt;
    }

    private const string DefaultSystemPrompt = "You are a concise assistant. Answer the user's selected text directly. Keep output ready to paste. Do not mention that text was selected or copied.";

    private static string? ExtractText(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString();
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var builder = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    builder.Append(text.GetString());
                }
            }
        }

        return builder.Length > 0 ? builder.ToString() : null;
    }

    private static string? ExtractError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string? ExtractChatText(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            return null;
        }

        var first = choices[0];
        if (first.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.String)
        {
            return content.GetString();
        }

        return null;
    }
}
