using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Milestones.Events;

namespace ProjectManagementSystem.Application.Milestones.EventHandlers;

/// <summary>
/// Reacts to the events of the Milestone aggregate by keeping its read models: the row of the milestone and its tasks, with the names of the
/// tasks and their packages. Statuses of the tasks and the progress are kept by the refresh of the project. Every event
/// sets the version of the milestone.
/// </summary>
public sealed class MilestoneEventHandler(
    IReadModelRepository<MilestoneItemView> milestoneItems,
    IReadModelRepository<MilestoneTaskItemView> milestoneTaskItems,
    IReadModelRepository<TaskItemView> taskItems,
    ProjectReadModelRefresher refresher) :
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
        var milestone = await milestoneItems.FindAsync(m => m.Id == domainEvent.MilestoneId, cancellationToken);
        if (milestone is null)
        {
            milestone = new MilestoneItemView { Id = domainEvent.MilestoneId, ProjectId = domainEvent.ProjectId, Status = MilestoneStatus.Open };
            milestoneItems.Add(milestone);
        }

        milestone.Name = domainEvent.Name;
        milestone.Description = domainEvent.Description ?? string.Empty;
        milestone.TargetDate = domainEvent.TargetDate;
        milestone.Version = Math.Max(milestone.Version, domainEvent.AggregateVersion);
    }

    public Task HandleAsync(MilestoneTaskAdded domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.MilestoneId, domainEvent, async milestone =>
        {
            if (await milestoneTaskItems.FindAsync(t => t.MilestoneId == milestone.Id && t.TaskId == domainEvent.TaskId, cancellationToken) is not null)
                return;

            var task = await taskItems.FindAsync(t => t.Id == domainEvent.TaskId, cancellationToken);
            milestoneTaskItems.Add(new MilestoneTaskItemView
            {
                MilestoneId = milestone.Id,
                TaskId = domainEvent.TaskId,
                ProjectId = milestone.ProjectId,
                WorkPackageId = task?.WorkPackageId ?? Guid.Empty,
                Name = task?.Name ?? string.Empty,
                WorkPackageDescription = task?.WorkPackageDescription ?? string.Empty,
                Position = (await milestoneTaskItems.ListAsync(t => t.MilestoneId == milestone.Id, cancellationToken)).Count + 1,
            });
        }, cancellationToken);

    public Task HandleAsync(MilestoneTaskRemoved domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.MilestoneId, domainEvent, async milestone =>
        {
            if (await milestoneTaskItems.FindAsync(t => t.MilestoneId == milestone.Id && t.TaskId == domainEvent.TaskId, cancellationToken) is { } task)
                milestoneTaskItems.Remove(task);
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
        if (await milestoneItems.FindAsync(m => m.Id == domainEvent.MilestoneId, cancellationToken) is { } milestone)
            milestoneItems.Remove(milestone);

        foreach (var task in await milestoneTaskItems.ListAsync(t => t.MilestoneId == domainEvent.MilestoneId, cancellationToken))
            milestoneTaskItems.Remove(task);
    }

    private async Task ChangeAsync(
        Guid milestoneId, IDomainEvent domainEvent, Func<MilestoneItemView, Task> change, CancellationToken cancellationToken)
    {
        var milestone = await milestoneItems.FindAsync(m => m.Id == milestoneId, cancellationToken);
        if (milestone is null)
            return;

        await change(milestone);
        milestone.Version = Math.Max(milestone.Version, domainEvent.AggregateVersion);
        await refresher.RefreshAsync(milestone.ProjectId, cancellationToken);
    }
}
