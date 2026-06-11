using Microsoft.Extensions.Logging.Abstractions;
using TicketTriage.Core.Abstractions;
using TicketTriage.Core.Exceptions;
using TicketTriage.Core.Models;
using TicketTriage.Core.Services;
using Xunit;

namespace TicketTriage.Tests;

public class TriageServiceTests
{
    private const string ValidJson = """
        {
          "category": "Billing",
          "priority": "High",
          "sentiment": "Angry",
          "summary": "Customer was double-charged for the monthly subscription.",
          "suggestedFirstResponse": "I am sorry about the duplicate charge - we are looking into it right away."
        }
        """;

    private static TriageService CreateService(string llmReply) =>
        new(new FakeLlmClient(llmReply), NullLogger<TriageService>.Instance);

    private static TicketRequest SampleTicket() =>
        new("Charged twice!", "You charged my card twice this month. Fix this now.", "Pro");

    [Fact]
    public async Task TriageAsync_ParsesValidJsonIntoTypedResult()
    {
        var service = CreateService(ValidJson);

        var result = await service.TriageAsync(SampleTicket());

        Assert.Equal(TicketCategory.Billing, result.Category);
        Assert.Equal(TicketPriority.High, result.Priority);
        Assert.Equal(Sentiment.Angry, result.Sentiment);
        Assert.False(string.IsNullOrWhiteSpace(result.Summary));
        Assert.False(string.IsNullOrWhiteSpace(result.SuggestedFirstResponse));
    }

    [Fact]
    public async Task TriageAsync_StripsMarkdownFences()
    {
        var fenced = $"```json\n{ValidJson}\n```";
        var service = CreateService(fenced);

        var result = await service.TriageAsync(SampleTicket());

        Assert.Equal(TicketCategory.Billing, result.Category);
    }

    [Fact]
    public async Task TriageAsync_ExtractsJsonSurroundedByProse()
    {
        var noisy = $"Sure! Here is the triage result:\n{ValidJson}\nLet me know if you need anything else.";
        var service = CreateService(noisy);

        var result = await service.TriageAsync(SampleTicket());

        Assert.Equal(TicketPriority.High, result.Priority);
    }

    [Fact]
    public async Task TriageAsync_ThrowsLlmOutputException_WhenNoJsonPresent()
    {
        var service = CreateService("I cannot help with that.");

        await Assert.ThrowsAsync<LlmOutputException>(() => service.TriageAsync(SampleTicket()));
    }

    [Fact]
    public async Task TriageAsync_ThrowsLlmOutputException_WhenJsonIsMalformed()
    {
        var service = CreateService("{ \"category\": \"Billing\", ");

        await Assert.ThrowsAsync<LlmOutputException>(() => service.TriageAsync(SampleTicket()));
    }

    private sealed class FakeLlmClient(string reply) : ILlmClient
    {
        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default) =>
            Task.FromResult(new LlmResponse(reply, new LlmUsage(100, 50), "Fake", "fake-model"));
    }
}
