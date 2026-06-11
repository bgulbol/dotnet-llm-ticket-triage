namespace TicketTriage.Core.Models;

public enum TicketCategory { Billing, Technical, Account, FeatureRequest, Other }

public enum TicketPriority { Low, Medium, High, Urgent }

public enum Sentiment { Positive, Neutral, Negative, Angry }

/// <summary>Structured triage decision produced by the LLM and validated by the service layer.</summary>
public record TriageResult(
    TicketCategory Category,
    TicketPriority Priority,
    Sentiment Sentiment,
    string Summary,
    string SuggestedFirstResponse);
