namespace ProjectManagementSystem.Infrastructure.Outbox;

/// <summary>
/// Delivers outbox messages in the order of their sequence until the queue is empty or waits for a failed message.
/// </summary>
internal interface IOutboxDrainer
{
    /// <returns>
    /// When the earliest undelivered message may be tried again, or <see langword="null"/> if nothing is left to deliver.
    /// </returns>
    Task<DateTime?> DrainAsync(CancellationToken cancellationToken);
}
