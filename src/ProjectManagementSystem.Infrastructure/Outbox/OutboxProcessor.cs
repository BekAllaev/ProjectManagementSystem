using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProjectManagementSystem.Infrastructure.Outbox;

/// <summary>
/// Delivers the outbox without polling: drains it once at start and then whenever <see cref="OutboxSignal"/> says a
/// commit added messages, or when a failed message may be tried again. While idle it does not touch the database.
/// A failure of the drain itself (database unavailable) never stops the host: it is logged and the drain is repeated
/// after its own backoff.
/// </summary>
internal sealed class OutboxProcessor(
    IOutboxDrainer drainer,
    OutboxSignal signal,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var drainFailures = 0;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime? nextAttemptAt;
                try
                {
                    nextAttemptAt = await drainer.DrainAsync(stoppingToken);
                    drainFailures = 0;
                }
                catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    drainFailures++;
                    var delay = OutboxOptions.Pick(options.Value.DrainFailureBackoff, drainFailures);
                    logger.LogError(exception, "Reading the outbox failed ({Failures} in a row); next try in {Delay}.", drainFailures, delay);

                    // Signals do not cut this wait short: with the database down every command would cause a new failing
                    // try. A signal sent meanwhile stays in the channel, and the next drain reads the whole outbox anyway.
                    await Task.Delay(delay, timeProvider, stoppingToken);
                    continue;
                }

                if (nextAttemptAt is null)
                    await signal.WaitAsync(stoppingToken);
                else
                    await WaitForSignalOrAsync(nextAttemptAt.Value, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping.
        }
    }

    private async Task WaitForSignalOrAsync(DateTime nextAttemptAt, CancellationToken stoppingToken)
    {
        var delay = nextAttemptAt - timeProvider.GetUtcNow().UtcDateTime;
        if (delay <= TimeSpan.Zero)
            return;

        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var signalled = signal.WaitAsync(waiting.Token);
        var elapsed = Task.Delay(delay, timeProvider, waiting.Token);

        await Task.WhenAny(signalled, elapsed);
        await waiting.CancelAsync();
        stoppingToken.ThrowIfCancellationRequested();
    }
}
