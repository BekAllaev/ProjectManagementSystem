using MediatR;
using Microsoft.Extensions.Logging;

namespace ProjectManagementSystem.Application.Behaviors;

/// <summary>
/// Outermost behavior: logs the name of the request, how long it took and whether it succeeded.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    TimeProvider timeProvider) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = timeProvider.GetTimestamp();

        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();

            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds} ms",
                requestName, timeProvider.GetElapsedTime(startedAt).TotalMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "{RequestName} failed after {ElapsedMilliseconds} ms: {ErrorMessage}",
                requestName, timeProvider.GetElapsedTime(startedAt).TotalMilliseconds, exception.Message);

            throw;
        }
    }
}
