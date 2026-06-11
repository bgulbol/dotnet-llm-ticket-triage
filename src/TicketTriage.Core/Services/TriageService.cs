using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TicketTriage.Core.Abstractions;
using TicketTriage.Core.Exceptions;
using TicketTriage.Core.Models;
using TicketTriage.Core.Prompts;

namespace TicketTriage.Core.Services;

public class TriageService(ILlmClient llm, ILogger<TriageService> logger) : ITriageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<TriageResult> TriageAsync(TicketRequest ticket, CancellationToken ct = default)
    {
        var request = new LlmRequest(TriagePrompts.System, BuildUserMessage(ticket));
        var response = await llm.CompleteAsync(request, ct);

        logger.LogInformation(
            "Ticket triaged via {Provider}/{Model} ({InputTokens} in / {OutputTokens} out tokens)",
            response.Provider, response.Model, response.Usage.InputTokens, response.Usage.OutputTokens);

        var json = ExtractJson(response.Content);

        TriageResult? result;
        try
        {
            result = JsonSerializer.Deserialize<TriageResult>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new LlmOutputException($"Model output is not valid JSON: {ex.Message}", response.Content);
        }

        return result ?? throw new LlmOutputException("Model output deserialized to null.", response.Content);
    }

    private static string BuildUserMessage(TicketRequest ticket)
    {
        var tier = string.IsNullOrWhiteSpace(ticket.CustomerTier) ? "Unknown" : ticket.CustomerTier;
        return $"""
            Customer tier: {tier}
            Subject: {ticket.Subject}
            Body:
            {ticket.Body}
            """;
    }

    /// <summary>
    /// Defensive extraction: even with "JSON only" instructions, models occasionally wrap output
    /// in markdown fences or add a stray sentence. We extract the outermost JSON object instead of failing.
    /// </summary>
    internal static string ExtractJson(string content)
    {
        var trimmed = content.Trim();

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
                trimmed = trimmed[(firstNewline + 1)..lastFence].Trim();
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new LlmOutputException("Model response did not contain a JSON object.", content);

        return trimmed[start..(end + 1)];
    }
}
