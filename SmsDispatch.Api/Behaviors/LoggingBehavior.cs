using MediatR;
using System.Diagnostics;

namespace SmsDispatch.Api.Behaviors;

/// <summary>
/// MediatR pipeline behavior: logs every request with its elapsed time.
/// Automatically wraps all Commands and Queries — no per-handler code needed.
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("→ Handling {Request}", name);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();
            logger.LogInformation("← {Request} completed in {Ms} ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "✗ {Request} failed after {Ms} ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
