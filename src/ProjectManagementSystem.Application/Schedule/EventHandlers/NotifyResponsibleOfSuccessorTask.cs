using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Notifications;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule.Events;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Schedule.EventHandlers;

/// <summary>
/// A predecessor of an unfinished task was cancelled and the dependency was removed: the responsible persons
/// of the successor task and of its work package are told, so they can re-plan. Only notifies.
/// </summary>
public sealed class NotifyResponsibleOfSuccessorTask(
    IRepository<WorkPackage> workPackages,
    INotificationSender notificationSender) : IDomainEventHandler<PredecessorCancelled>
{
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
}
