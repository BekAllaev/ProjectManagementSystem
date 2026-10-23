using Microsoft.Extensions.Configuration;

namespace ProjectManagementSystem.Infrastructure.Outbox;

/// <summary>
/// Settings of the outbox processor, section <c>Outbox</c>. A list given in the configuration replaces the default one.
/// </summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// Waits before the next delivery of a failed message, by the number of failures so far; the last one repeats.
    /// </summary>
    public IReadOnlyList<TimeSpan> RetryBackoff { get; set; } =
        [TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10), TimeSpan.FromHours(1)];

    /// <summary>
    /// After this many failed deliveries of one message its event type is parked.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Waits after a failure of reading the outbox itself (database unavailable), by the number of failures in a row;
    /// the last one repeats. Not related to failures of single messages.
    /// </summary>
    public IReadOnlyList<TimeSpan> DrainFailureBackoff { get; set; } =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10)];

    /// <summary>
    /// Processed messages older than this are deleted.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Binds the section. A plain bind would append a configured list to the default one, so lists are replaced.
    /// </summary>
    internal void Bind(IConfiguration section)
    {
        var retryBackoff = section.GetSection(nameof(RetryBackoff)).Get<TimeSpan[]>();
        var drainFailureBackoff = section.GetSection(nameof(DrainFailureBackoff)).Get<TimeSpan[]>();

        section.Bind(this);

        if (retryBackoff is { Length: > 0 })
            RetryBackoff = retryBackoff;
        if (drainFailureBackoff is { Length: > 0 })
            DrainFailureBackoff = drainFailureBackoff;
    }

    internal bool IsValid() =>
        RetryBackoff.Count > 0 && DrainFailureBackoff.Count > 0 && MaxAttempts > 0 && RetentionPeriod > TimeSpan.Zero;

    internal static TimeSpan Pick(IReadOnlyList<TimeSpan> backoff, int failures) =>
        backoff[Math.Clamp(failures - 1, 0, backoff.Count - 1)];
}
