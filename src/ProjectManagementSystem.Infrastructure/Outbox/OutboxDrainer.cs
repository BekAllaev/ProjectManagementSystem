using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.Persistence.Outbox;

namespace ProjectManagementSystem.Infrastructure.Outbox;

/// <summary>
/// Delivers messages one by one in the order of <see cref="OutboxMessage.Sequence"/>, each in its own scope. A failed
/// message holds the queue until its next attempt (strict order). After <see cref="OutboxOptions.MaxAttempts"/>
/// failures its event type is parked: messages of that type wait for a person, the rest of the queue goes on.
/// </summary>
internal sealed class OutboxDrainer(
    IServiceScopeFactory scopeFactory,
    DomainEventSerializer serializer,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDrainer> logger) : IOutboxDrainer
{
    public async Task<DateTime?> DrainAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var message = await FindNextAsync(cancellationToken);
            if (message is null || message.NextAttemptAt > Now)
            {
                await DeleteOldProcessedAsync(cancellationToken);
                return message?.NextAttemptAt;
            }

            try
            {
                await DeliverAsync(message, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                await RecordFailureAsync(message.Sequence, exception, cancellationToken);
            }
        }
    }

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    private async Task<OutboxMessage?> FindNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(m => m.ProcessedAt == null && m.ParkedAt == null)
            .Where(m => !dbContext.ParkedEventTypes.Any(p => p.EventType == m.EventType))
            .OrderBy(m => m.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Runs every handler of the event, then marks the message processed. Notifications added by the handlers are saved
    /// in the same <c>SaveChanges</c>, so for handlers that only notify the delivery is atomic. Handlers that change
    /// aggregates save them earlier themselves; if the delivery fails after that, the message is delivered again and the
    /// handlers, being idempotent, change nothing.
    /// </summary>
    private async Task DeliverAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var domainEvent = serializer.Deserialize(message.EventType, message.Payload);
        await services.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(domainEvent, cancellationToken);

        var dbContext = services.GetRequiredService<AppDbContext>();
        var tracked = await dbContext.OutboxMessages.SingleAsync(m => m.Sequence == message.Sequence, cancellationToken);
        tracked.ProcessedAt = Now;

        await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
    }

    private async Task RecordFailureAsync(long sequence, Exception exception, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var message = await dbContext.OutboxMessages.SingleAsync(m => m.Sequence == sequence, cancellationToken);
        message.Attempts++;
        message.LastError = exception.ToString();

        if (message.Attempts >= options.Value.MaxAttempts)
        {
            message.ParkedAt = Now;
            message.NextAttemptAt = null;

            if (!await dbContext.ParkedEventTypes.AnyAsync(p => p.EventType == message.EventType, cancellationToken))
            {
                dbContext.ParkedEventTypes.Add(new ParkedEventType
                {
                    EventType = message.EventType,
                    ParkedAt = Now,
                    Reason = $"Message {message.Sequence} (event {message.EventId}) failed {message.Attempts} times: {exception.Message}",
                });
            }

            logger.LogError(
                exception,
                "Outbox message {Sequence} ({EventType}) failed {Attempts} times; event type {EventType} is parked.",
                message.Sequence, message.EventType, message.Attempts, message.EventType);
        }
        else
        {
            var delay = OutboxOptions.Pick(options.Value.RetryBackoff, message.Attempts);
            message.NextAttemptAt = Now + delay;

            logger.LogWarning(
                exception,
                "Outbox message {Sequence} ({EventType}) failed (attempt {Attempts}); next attempt in {Delay}.",
                message.Sequence, message.EventType, message.Attempts, delay);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Cleanup at the end of every drain, so no separate timer is needed.
    /// </summary>
    private async Task DeleteOldProcessedAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var threshold = Now - options.Value.RetentionPeriod;

        await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt != null && m.ProcessedAt < threshold)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
