namespace TicketTriage.Core.Exceptions;

/// <summary>Base exception for LLM provider failures (HTTP errors, rate limits after retries, etc.).</summary>
public class LlmException(string message) : Exception(message);

/// <summary>Thrown when the model responds, but the content cannot be parsed into the expected schema.</summary>
public class LlmOutputException(string message, string? rawContent = null) : LlmException(message)
{
    /// <summary>Raw model output, kept for diagnostics/logging. Never returned to API consumers.</summary>
    public string? RawContent { get; } = rawContent;
}
