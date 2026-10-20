using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.TimeTracking.Events;

/// <summary>
/// The read side upserts a row by <paramref name="EntryId"/>, so a repeated delivery does not double the cost.
/// </summary>
public sealed record TimeLogged(
    Guid ProjectId,
    Guid WorkPackageId,
    Guid TaskId,
    Guid EntryId,
    Guid EmployeeId,
    DateOnly WorkDate,
    decimal Hours,
    decimal HourlyRate) : DomainEvent;

public sealed record TimeEntryRemoved(Guid ProjectId, Guid TaskId, Guid EntryId) : DomainEvent;
