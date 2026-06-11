using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketTriage.Core.Abstractions;
using TicketTriage.Core.Exceptions;

namespace TicketTriage.Providers;

/// <summary>
/// OpenAI Chat Completions client. Uses raw HttpClient on purpose: no SDK version churn,
/// full control over the request, and it shows exactly what goes over the wire.
/// </summary>
public sealed class OpenAiClient(
    HttpClient http,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiClient> logger) : HttpLlmClientBase(logger), ILlmClient
{
    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.ApiKey))
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Set the OPENAI_API_KEY environment variable or Llm:OpenAi:ApiKey.");

        HttpRequestMessage CreateRequest()
        {
            var message = new HttpRequestMessage(HttpMethod.Post, $"{o.BaseUrl}/chat/completions");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey);
            message.Content = JsonContent.Create(new
            {
                model = o.Model,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserMessage }
                }
            });
            return message;
        }

        using var response = await SendWithRetryAsync(CreateRequest, http, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new LlmException($"OpenAI API returned {(int)response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        var usage = root.TryGetProperty("usage", out var u)
            ? new LlmUsage(u.GetProperty("prompt_tokens").GetInt32(), u.GetProperty("completion_tokens").GetInt32())
            : new LlmUsage(0, 0);

        return new LlmResponse(content, usage, "OpenAI", o.Model);
    }
}
