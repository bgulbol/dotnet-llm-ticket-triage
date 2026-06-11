using System.Text.Json.Serialization;
using TicketTriage.Core.Abstractions;
using TicketTriage.Core.Exceptions;
using TicketTriage.Core.Models;
using TicketTriage.Core.Services;
using TicketTriage.Providers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddLlm(builder.Configuration);
builder.Services.AddScoped<ITriageService, TriageService>();

var app = builder.Build();

app.MapOpenApi();

app.MapPost("/api/triage", async (TicketRequest ticket, ITriageService triage, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(ticket.Subject) && string.IsNullOrWhiteSpace(ticket.Body))
        return Results.BadRequest(new { error = "Subject or body is required." });

    try
    {
        var result = await triage.TriageAsync(ticket, ct);
        return Results.Ok(result);
    }
    catch (LlmOutputException ex)
    {
        // The model answered, but not in the expected schema. Surface a clean 502 instead of a raw 500.
        return Results.Problem(
            title: "LLM returned an unusable response",
            detail: ex.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (LlmException ex)
    {
        return Results.Problem(
            title: "LLM provider error",
            detail: ex.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("TriageTicket")
.WithSummary("Analyzes a support ticket and returns category, priority, sentiment, a summary and a suggested first response.");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health");

app.Run();
