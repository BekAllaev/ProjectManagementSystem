using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.ReadModels;

namespace ProjectManagementSystem.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(
    IReadModelRepository<NotificationItemView> notifications,
    IReadModelRepository<MembershipPeriodView> periods,
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<TaskItemView> tasks,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IQueryHandler<GetNotificationsQuery, NotificationList>
{
    public async Task<NotificationList> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var inbox = await NotificationInbox.ListAsync(notifications, periods, currentUser.EmployeeId, timeProvider.Today(), cancellationToken);

        var selected = inbox
            .Where(n => !query.UnreadOnly || n.ReadAt is null)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(query.Take ?? int.MaxValue)
            .ToList();

        var projectIds = selected.Select(n => n.ProjectId).Distinct().ToList();
        var titles = (await projects.ListAsync(p => projectIds.Contains(p.Id), cancellationToken)).ToDictionary(p => p.Id, p => p.Title);
        var taskIds = selected.Where(n => n.TaskId is not null).Select(n => n.TaskId!.Value).Distinct().ToList();
        var taskNames = (await tasks.ListAsync(t => taskIds.Contains(t.Id), cancellationToken)).ToDictionary(t => t.Id, t => t.Name);

        var items = selected
            .Select(n => new NotificationItem(
                n.Id,
                n.Kind,
                n.Title,
                n.Message,
                n.ProjectId,
                titles.GetValueOrDefault(n.ProjectId, string.Empty),
                n.WorkPackageId,
                n.TaskId,
                n.TaskId is { } taskId ? taskNames.GetValueOrDefault(taskId) : null,
                n.RecipientEmployeeId is null,
                n.CreatedAt,
                n.ReadAt is not null))
            .ToList();

        return new NotificationList(items, inbox.Count(n => n.ReadAt is null));
    }
}
