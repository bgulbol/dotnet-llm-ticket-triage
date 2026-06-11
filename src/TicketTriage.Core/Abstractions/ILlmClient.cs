namespace TicketTriage.Core.Abstractions;

/// <summary>
/// Provider-agnostic LLM completion client. Implementations exist for OpenAI and Anthropic;
/// swapping providers is a configuration change, not a code change.
/// </summary>
public interface ILlmClient
{
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default);
}

public record LlmRequest(
    string SystemPrompt,
    string UserMessage,
    int MaxTokens = 1024,
    double Temperature = 0.2);

public record LlmResponse(string Content, LlmUsage Usage, string Provider, string Model);

public record LlmUsage(int InputTokens, int OutputTokens);
