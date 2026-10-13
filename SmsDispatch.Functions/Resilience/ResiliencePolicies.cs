using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;

namespace SmsDispatch.Functions.Resilience;

public static class ResiliencePolicies
{
    // ── SMS API policies ──────────────────────────────────────────────────────

    public static IAsyncPolicy<HttpResponseMessage> SmsApiRetry(ILogger logger)
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, delay, attempt, _) =>
                    logger.LogWarning(
                        "SMS API retry {Attempt}/3 after {Delay}s — {Reason}",
                        attempt, delay.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()));

    public static IAsyncPolicy<HttpResponseMessage> SmsApiCircuitBreaker(ILogger logger)
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                    logger.LogError(
                        "SMS API circuit OPENED for {Seconds}s — {Reason}",
                        breakDelay.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()),
                onReset: () =>
                    logger.LogInformation("SMS API circuit RESET — resuming normal calls"),
                onHalfOpen: () =>
                    logger.LogInformation("SMS API circuit HALF-OPEN — probing with one call"));

    /// <summary>Circuit breaker (outer) wraps retry (inner).</summary>
    public static IAsyncPolicy<HttpResponseMessage> SmsApiPolicy(ILogger logger)
        => Policy.WrapAsync(SmsApiCircuitBreaker(logger), SmsApiRetry(logger));

    // ── Web API (status callback) policies ────────────────────────────────────

    public static IAsyncPolicy<HttpResponseMessage> WebApiRetry(ILogger logger)
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                onRetry: (outcome, delay, attempt, _) =>
                    logger.LogWarning(
                        "Web API status update retry {Attempt}/3 after {Delay}s — {Reason}",
                        attempt, delay.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()));

    public static IAsyncPolicy<HttpResponseMessage> WebApiCircuitBreaker(ILogger logger)
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(15),
                onBreak: (outcome, breakDelay) =>
                    logger.LogError(
                        "Web API circuit OPENED for {Seconds}s — {Reason}",
                        breakDelay.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()),
                onReset: () =>
                    logger.LogInformation("Web API circuit RESET"),
                onHalfOpen: () =>
                    logger.LogInformation("Web API circuit HALF-OPEN — probing"));

    /// <summary>Circuit breaker (outer) wraps retry (inner).</summary>
    public static IAsyncPolicy<HttpResponseMessage> WebApiPolicy(ILogger logger)
        => Policy.WrapAsync(WebApiCircuitBreaker(logger), WebApiRetry(logger));
}
