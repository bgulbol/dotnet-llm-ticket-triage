using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketTriage.Core.Abstractions;
using TicketTriage.Core.Exceptions;

namespace TicketTriage.Providers;

/// <summary>Anthropic Messages API client.</summary>
public sealed class AnthropicClient(
    HttpClient http,
    IOptions<AnthropicOptions> options,
    ILogger<AnthropicClient> logger) : HttpLlmClientBase(logger), ILlmClient
{
    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.ApiKey))
            throw new InvalidOperationException(
                "Anthropic API key is not configured. Set the ANTHROPIC_API_KEY environment variable or Llm:Anthropic:ApiKey.");

        HttpRequestMessage CreateRequest()
        {
            var message = new HttpRequestMessage(HttpMethod.Post, $"{o.BaseUrl}/messages");
            message.Headers.Add("x-api-key", o.ApiKey);
            message.Headers.Add("anthropic-version", o.ApiVersion);
            message.Content = JsonContent.Create(new
            {
                model = o.Model,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                system = request.SystemPrompt,
                messages = new object[]
                {
                    new { role = "user", content = request.UserMessage }
                }
            });
            return message;
        }

        using var response = await SendWithRetryAsync(CreateRequest, http, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new LlmException($"Anthropic API returned {(int)response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var content = root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
        var usage = root.TryGetProperty("usage", out var u)
            ? new LlmUsage(u.GetProperty("input_tokens").GetInt32(), u.GetProperty("output_tokens").GetInt32())
            : new LlmUsage(0, 0);

        return new LlmResponse(content, usage, "Anthropic", o.Model);
    }
}
