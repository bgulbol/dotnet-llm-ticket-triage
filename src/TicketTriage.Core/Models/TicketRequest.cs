namespace TicketTriage.Core.Models;

/// <summary>An incoming support ticket to be triaged.</summary>
public record TicketRequest(string Subject, string Body, string? CustomerTier = null);
