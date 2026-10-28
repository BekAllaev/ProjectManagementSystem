using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Milestones.Events;

namespace ProjectManagementSystem.Infrastructure.ReadModels.Projections;

/// <summary>
/// Keeps the read models of the Milestone aggregate: the row of the milestone and its tasks, with the names of the
/// tasks and their packages. Statuses of the tasks and the progress are kept by the refresh of the project. Every event
/// sets the version of the milestone.
/// </summary>
internal sealed class MilestoneProjection(ReadModelStore store, ProjectReadModelRefresher refresher) :
    IDomainEventHandler<MilestoneCreated>,
    IDomainEventHandler<MilestoneTaskAdded>,
    IDomainEventHandler<MilestoneTaskRemoved>,
    IDomainEventHandler<MilestoneTargetDateChanged>,
    IDomainEventHandler<MilestoneRenamed>,
    IDomainEventHandler<MilestoneCompleted>,
    IDomainEventHandler<MilestoneDeleted>
{
    public async Task HandleAsync(MilestoneCreated domainEvent, CancellationToken cancellationToken = default)
    {
        var milestone = await store.FindAsync<MilestoneItemView>(cancellationToken, domainEvent.MilestoneId);
        if (milestone is null)
        {
            milestone = new MilestoneItemView { Id = domainEvent.MilestoneId, ProjectId = domainEvent.ProjectId, Status = MilestoneStatus.Open };
            store.Add(milestone);
        }

        milestone.Name = domainEvent.Name;
        milestone.Description = domainEvent.Description ?? string.Empty;
        milestone.TargetDate = domainEvent.TargetDate;
        milestone.Version = Math.Max(milestone.Version, domainEvent.AggregateVersion);
    }

    public Task HandleAsync(MilestoneTaskAdded domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.MilestoneId, domainEvent, async milestone =>
        {
            if (await store.FindAsync<MilestoneTaskItemView>(cancellationToken, milestone.Id, domainEvent.TaskId) is not null)
                return;

            var task = await store.FindAsync<TaskItemView>(cancellationToken, domainEvent.TaskId);
            store.Add(new MilestoneTaskItemView
            {
                MilestoneId = milestone.Id,
                TaskId = domainEvent.TaskId,
                ProjectId = milestone.ProjectId,
                WorkPackageId = task?.WorkPackageId ?? Guid.Empty,
                Name = task?.Name ?? string.Empty,
                WorkPackageDescription = task?.WorkPackageDescription ?? string.Empty,
                Position = (await store.LoadAsync<MilestoneTaskItemView>(t => t.MilestoneId == milestone.Id, cancellationToken)).Count + 1,
            });
        }, cancellationToken);

    public Task HandleAsync(MilestoneTaskRemoved domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.MilestoneId, domainEvent, async milestone =>
        {
            if (await store.FindAsync<MilestoneTaskItemView>(cancellationToken, milestone.Id, domainEvent.TaskId) is { } task)
                store.Remove(task);
        }, cancellationToken);

    public Task HandleAsync(MilestoneTargetDateChanged domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.MilestoneId, domainEvent, milestone =>
        {
            milestone.TargetDate = domainEvent.TargetDate;
            return Task.CompletedTask;
        }, cancellationToken);

    public Task HandleAsync(MilestoneRenamed domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.MilestoneId, domainEvent, milestone =>
        {
            milestone.Name = domainEvent.Name;
            return Task.CompletedTask;
        }, cancellationToken);

    public Task HandleAsync(MilestoneCompleted domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.MilestoneId, domainEvent, milestone =>
        {
            milestone.Status = MilestoneStatus.Completed;
            milestone.CompletedAt = domainEvent.CompletedAt;
            return Task.CompletedTask;
        }, cancellationToken);

    public async Task HandleAsync(MilestoneDeleted domainEvent, CancellationToken cancellationToken = default)
    {
        if (await store.FindAsync<MilestoneItemView>(cancellationToken, domainEvent.MilestoneId) is { } milestone)
            store.Remove(milestone);

        foreach (var task in await store.LoadAsync<MilestoneTaskItemView>(t => t.MilestoneId == domainEvent.MilestoneId, cancellationToken))
            store.Remove(task);
    }

    private async Task ChangeAsync(
        Guid milestoneId, IDomainEvent domainEvent, Func<MilestoneItemView, Task> change, CancellationToken cancellationToken)
    {
        var milestone = await store.FindAsync<MilestoneItemView>(cancellationToken, milestoneId);
        if (milestone is null)
            return;

        await change(milestone);
        milestone.Version = Math.Max(milestone.Version, domainEvent.AggregateVersion);
        await refresher.RefreshAsync(milestone.ProjectId, cancellationToken);
    }
}
