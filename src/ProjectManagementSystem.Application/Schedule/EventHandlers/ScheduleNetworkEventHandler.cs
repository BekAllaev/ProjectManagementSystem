using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Notifications;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Schedule.Events;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Schedule.EventHandlers;

/// <summary>
/// Reacts to the events of the ScheduleNetwork aggregate:
/// <list type="bullet">
/// <item>keeps the dependency read models, with the names of both tasks and their packages, which never change;
/// statuses and warnings are kept by the refresh of the project, and the version of the network by the row of the
/// project;</item>
/// <item>removes a dependency added concurrently with the cancellation of one of its tasks, as if it had existed before
/// the cancellation;</item>
/// <item>notifies the responsible persons of an unfinished successor whose predecessor was cancelled.</item>
/// </list>
/// A repeated delivery changes nothing: rows are added once, the removal is a no-op the second time, notifications have
/// keys.
/// </summary>
public sealed class ScheduleNetworkEventHandler(
    IReadModelRepository<DependencyItemView> dependencyItems,
    IReadModelRepository<TaskItemView> taskItems,
    IReadModelRepository<ProjectItemView> projectItems,
    ProjectReadModelRefresher refresher,
    IRepository<WorkPackage> workPackages,
    IRepository<ScheduleNetwork> networks,
    INotificationSender notificationSender,
    IUnitOfWork unitOfWork) :
    IDomainEventHandler<DependencyAdded>,
    IDomainEventHandler<DependencyRemoved>,
    IDomainEventHandler<PredecessorCancelled>
{
    public async Task HandleAsync(DependencyAdded domainEvent, CancellationToken cancellationToken = default)
    {
        await ProjectAsync(domainEvent, cancellationToken);
        await RemoveIfATaskIsAlreadyCancelledAsync(domainEvent, cancellationToken);
    }

    /// <summary>
    /// Only the change of the read models; also used to rebuild them.
    /// </summary>
    public async Task ProjectAsync(DependencyAdded domainEvent, CancellationToken cancellationToken = default)
    {
        var dependency = domainEvent.Dependency;

        if (await FindAsync(domainEvent.ProjectId, dependency, cancellationToken) is null)
        {
            var predecessor = await taskItems.FindAsync(t => t.Id == dependency.PredecessorTaskId, cancellationToken);
            var successor = await taskItems.FindAsync(t => t.Id == dependency.SuccessorTaskId, cancellationToken);

            dependencyItems.Add(new DependencyItemView
            {
                ProjectId = domainEvent.ProjectId,
                PredecessorTaskId = dependency.PredecessorTaskId,
                PredecessorWorkPackageId = dependency.PredecessorWorkPackageId,
                PredecessorName = predecessor?.Name ?? string.Empty,
                PredecessorWorkPackageDescription = predecessor?.WorkPackageDescription ?? string.Empty,
                SuccessorTaskId = dependency.SuccessorTaskId,
                SuccessorWorkPackageId = dependency.SuccessorWorkPackageId,
                SuccessorName = successor?.Name ?? string.Empty,
                SuccessorWorkPackageDescription = successor?.WorkPackageDescription ?? string.Empty,
                Type = dependency.Type,
                IsAcrossWorkPackages = dependency.PredecessorWorkPackageId != dependency.SuccessorWorkPackageId,
            });
        }

        await ChangedAsync(domainEvent.ProjectId, domainEvent, cancellationToken);
    }

    public async Task HandleAsync(DependencyRemoved domainEvent, CancellationToken cancellationToken = default)
    {
        if (await FindAsync(domainEvent.ProjectId, domainEvent.Dependency, cancellationToken) is { } row)
            dependencyItems.Remove(row);

        await ChangedAsync(domainEvent.ProjectId, domainEvent, cancellationToken);
    }

    /// <summary>
    /// A predecessor of an unfinished task was cancelled and the dependency was removed (its read model goes with
    /// <see cref="DependencyRemoved"/>): the responsible persons of the successor task and of its work package are
    /// told, so they can re-plan. Only notifies.
    /// </summary>
    public async Task HandleAsync(PredecessorCancelled domainEvent, CancellationToken cancellationToken = default)
    {
        var dependency = domainEvent.Dependency;

        var workPackage = await workPackages.FindAsync(dependency.SuccessorWorkPackageId, cancellationToken);
        var task = workPackage?.Tasks.SingleOrDefault(t => t.Id == dependency.SuccessorTaskId);
        if (workPackage is null || task is null || task.IsFinished)
            return;

        foreach (var recipientId in new[] { task.ResponsiblePersonId, workPackage.ResponsiblePersonId }.Distinct())
        {
            await notificationSender.SendAsync(
                new Notification(
                    $"predecessor-cancelled:{domainEvent.EventId}:{recipientId}",
                    domainEvent.ProjectId,
                    recipientId,
                    NotificationKind.PredecessorCancelled,
                    "A predecessor task was cancelled",
                    $"Predecessor task '{dependency.PredecessorTaskId}' of task '{task.Name}' was cancelled; " +
                    "the dependency was removed. Check the plan of the task.",
                    workPackage.Id,
                    task.Id),
                cancellationToken);
        }
    }

    /// <summary>
    /// A task can be cancelled concurrently with adding a dependency to it, after the domain service has checked it.
    /// Then the dependency is removed here, as if it had existed before the cancellation.
    /// </summary>
    private async Task RemoveIfATaskIsAlreadyCancelledAsync(DependencyAdded domainEvent, CancellationToken cancellationToken)
    {
        var dependency = domainEvent.Dependency;
        var cancelledTaskIds = new List<Guid>();

        if (await IsCancelledAsync(dependency.PredecessorWorkPackageId, dependency.PredecessorTaskId, cancellationToken))
            cancelledTaskIds.Add(dependency.PredecessorTaskId);

        if (await IsCancelledAsync(dependency.SuccessorWorkPackageId, dependency.SuccessorTaskId, cancellationToken))
            cancelledTaskIds.Add(dependency.SuccessorTaskId);

        if (cancelledTaskIds.Count == 0)
            return;

        var network = await networks.FindAsync(domainEvent.ProjectId, cancellationToken);
        if (network is null)
            return;

        network.RemoveDependenciesOf(cancelledTaskIds);
        networks.Update(network);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsCancelledAsync(Guid workPackageId, Guid taskId, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.FindAsync(workPackageId, cancellationToken);

        return workPackage?.Tasks.SingleOrDefault(t => t.Id == taskId)?.Status == ProjectTaskStatus.Cancelled;
    }

    private Task<DependencyItemView?> FindAsync(Guid projectId, Dependency dependency, CancellationToken cancellationToken) =>
        dependencyItems.FindAsync(
            d => d.ProjectId == projectId &&
                 d.PredecessorTaskId == dependency.PredecessorTaskId &&
                 d.SuccessorTaskId == dependency.SuccessorTaskId,
            cancellationToken);

    private async Task ChangedAsync(Guid projectId, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (await projectItems.FindAsync(p => p.Id == projectId, cancellationToken) is { } project)
            project.ScheduleVersion = Math.Max(project.ScheduleVersion, domainEvent.AggregateVersion);

        await refresher.RefreshAsync(projectId, cancellationToken);
    }
}
