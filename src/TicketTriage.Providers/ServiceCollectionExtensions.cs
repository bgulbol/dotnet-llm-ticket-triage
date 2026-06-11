using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketTriage.Core.Abstractions;

namespace TicketTriage.Providers;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the LLM client selected by configuration (Llm:Provider = "OpenAI" | "Anthropic").
    /// API keys are resolved from configuration first, then from conventional environment variables,
    /// so secrets never need to live in appsettings.json.
    /// </summary>
    public static IServiceCollection AddLlm(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Llm:Provider"] ?? "OpenAI";

        if (provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<AnthropicOptions>(configuration.GetSection("Llm:Anthropic"));
            services.PostConfigure<AnthropicOptions>(o =>
                o.ApiKey ??= Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
            services.AddHttpClient<ILlmClient, AnthropicClient>(c => c.Timeout = TimeSpan.FromSeconds(60));
        }
        else
        {
            services.Configure<OpenAiOptions>(configuration.GetSection("Llm:OpenAi"));
            services.PostConfigure<OpenAiOptions>(o =>
                o.ApiKey ??= Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
            services.AddHttpClient<ILlmClient, OpenAiClient>(c => c.Timeout = TimeSpan.FromSeconds(60));
        }

        return services;
    }
}
