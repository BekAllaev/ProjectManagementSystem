using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.TimeTracking.Events;

namespace ProjectManagementSystem.Infrastructure.ReadModels.Projections;

/// <summary>
/// Keeps the time entries of the TaskTimeLog aggregate: a row is upserted by its entry id and deleted when the entry
/// is removed, so a repeated delivery does not double any cost. The refresh of the project then sums the logged time
/// of the task, the actual cost of the project and the cost by member and by work package. Every event sets the
/// version of the time log on its task.
/// </summary>
internal sealed class TimeTrackingProjection(ReadModelStore store, ProjectReadModelRefresher refresher) :
    IDomainEventHandler<TimeLogged>,
    IDomainEventHandler<TimeEntryRemoved>
{
    public async Task HandleAsync(TimeLogged domainEvent, CancellationToken cancellationToken = default)
    {
        var entry = await store.FindAsync<TimeEntryItemView>(cancellationToken, domainEvent.EntryId);
        if (entry is null)
        {
            entry = new TimeEntryItemView { Id = domainEvent.EntryId };
            store.Add(entry);
        }

        entry.ProjectId = domainEvent.ProjectId;
        entry.WorkPackageId = domainEvent.WorkPackageId;
        entry.TaskId = domainEvent.TaskId;
        entry.EmployeeId = domainEvent.EmployeeId;
        entry.WorkDate = domainEvent.WorkDate;
        entry.Hours = domainEvent.Hours;
        entry.HourlyRate = domainEvent.HourlyRate;
        entry.Cost = domainEvent.Hours * domainEvent.HourlyRate;

        await ChangedAsync(domainEvent.ProjectId, domainEvent.TaskId, domainEvent, cancellationToken);
    }

    public async Task HandleAsync(TimeEntryRemoved domainEvent, CancellationToken cancellationToken = default)
    {
        if (await store.FindAsync<TimeEntryItemView>(cancellationToken, domainEvent.EntryId) is { } entry)
            store.Remove(entry);

        await ChangedAsync(domainEvent.ProjectId, domainEvent.TaskId, domainEvent, cancellationToken);
    }

    private async Task ChangedAsync(Guid projectId, Guid taskId, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (await store.FindAsync<TaskItemView>(cancellationToken, taskId) is { } task)
            task.TimeLogVersion = Math.Max(task.TimeLogVersion, domainEvent.AggregateVersion);

        await refresher.RefreshAsync(projectId, cancellationToken);
    }
}
