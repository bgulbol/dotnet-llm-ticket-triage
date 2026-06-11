namespace TicketTriage.Providers;

public sealed class OpenAiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

public sealed class AnthropicOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-sonnet-4-6";
    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1";
    public string ApiVersion { get; set; } = "2023-06-01";
}
