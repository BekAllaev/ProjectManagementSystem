using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.TimeTracking.Events;

namespace ProjectManagementSystem.Domain.TimeTracking;

/// <summary>
/// Time logged on one task; its id is the id of the task. Created on the first logged entry.
/// Changed only through <see cref="Services.TimeTrackingService"/>, which checks the rules that need
/// the task (it must be InProgress) and the team (membership and hourly rate on the work date).
/// A wrong entry is fixed by removing it and logging the right one.
/// </summary>
public sealed class TaskTimeLog : AggregateRoot<Guid>
{
    private readonly List<TimeEntry> _entries = [];

    public Guid TaskId => Id;

    public Guid WorkPackageId { get; }

    public Guid ProjectId { get; }

    public IReadOnlyCollection<TimeEntry> Entries => _entries.AsReadOnly();

    internal TaskTimeLog(Guid taskId, Guid workPackageId, Guid projectId) : base(taskId)
    {
        WorkPackageId = workPackageId;
        ProjectId = projectId;
    }

    internal TimeEntry LogTime(Guid employeeId, DateOnly workDate, decimal hours, decimal hourlyRate)
    {
        var entry = new TimeEntry(Guid.NewGuid(), employeeId, workDate, hours, hourlyRate);
        _entries.Add(entry);

        RaiseDomainEvent(new TimeLogged(
            ProjectId, WorkPackageId, TaskId, entry.Id, entry.EmployeeId, entry.WorkDate, entry.Hours, entry.HourlyRate));
        return entry;
    }

    internal void RemoveEntry(Guid entryId)
    {
        var entry = _entries.SingleOrDefault(e => e.Id == entryId)
            ?? throw new EntityNotFoundException(nameof(TimeEntry), entryId);

        _entries.Remove(entry);
        RaiseDomainEvent(new TimeEntryRemoved(ProjectId, TaskId, entry.Id));
    }
}
