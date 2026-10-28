namespace ProjectManagementSystem.Application.Abstractions;

public static class TimeProviderExtensions
{
    /// <summary>
    /// The current date in UTC, as the domain services count it.
    /// </summary>
    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
