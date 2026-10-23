namespace ProjectManagementSystem.Application.Exceptions;

/// <summary>
/// Thrown by the unit of work when an aggregate was changed by someone else since it was loaded
/// (optimistic concurrency): nothing of the command is saved, and it can be repeated on fresh data.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public string AggregateName { get; }

    public Guid Id { get; }

    public ConcurrencyConflictException(string aggregateName, Guid id, Exception? innerException = null)
        : base($"{aggregateName} with id '{id}' was changed by someone else since it was loaded.", innerException)
    {
        AggregateName = aggregateName;
        Id = id;
    }
}
