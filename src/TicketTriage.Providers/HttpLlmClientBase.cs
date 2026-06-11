using System.Net;
using Microsoft.Extensions.Logging;

namespace TicketTriage.Providers;

/// <summary>
/// Shared HTTP plumbing for LLM providers: exponential-backoff retry on transient failures
/// (429 / 5xx). Request messages are created per attempt via a factory because
/// HttpRequestMessage instances cannot be reused.
/// </summary>
public abstract class HttpLlmClientBase(ILogger logger)
{
    private const int MaxAttempts = 3;

    protected async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        HttpClient http,
        CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            var response = await http.SendAsync(requestFactory(), ct);

            if (response.IsSuccessStatusCode || attempt >= MaxAttempts || !IsTransient(response.StatusCode))
                return response;

            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s
            logger.LogWarning(
                "LLM request failed with {StatusCode}; retrying in {DelaySeconds}s (attempt {Attempt}/{MaxAttempts})",
                (int)response.StatusCode, delay.TotalSeconds, attempt, MaxAttempts);

            response.Dispose();
            await Task.Delay(delay, ct);
        }
    }

    private static bool IsTransient(HttpStatusCode status) => status
        is HttpStatusCode.TooManyRequests
        or HttpStatusCode.InternalServerError
        or HttpStatusCode.BadGateway
        or HttpStatusCode.ServiceUnavailable
        or HttpStatusCode.GatewayTimeout;
}
