using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Notifications.Queries.GetNotifications;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Notifications;

internal sealed class GetNotificationsQueryHandler(AppDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    : IQueryHandler<GetNotificationsQuery, NotificationList>
{
    public async Task<NotificationList> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var inbox = (await dbContext.NotificationsOfAsync(currentUser.EmployeeId, timeProvider.Today(), cancellationToken)).AsNoTracking();

        var unreadCount = await inbox.CountAsync(n => n.ReadAt == null, cancellationToken);

        var selected = query.UnreadOnly ? inbox.Where(n => n.ReadAt == null) : inbox;
        selected = selected.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id);
        if (query.Take is { } take)
            selected = selected.Take(take);
        var notifications = await selected.ToListAsync(cancellationToken);

        var projects = await dbContext.ProjectItems(notifications.Select(n => n.ProjectId).Distinct().ToList())
            .ToDictionaryAsync(p => p.Id, p => p.Title, cancellationToken);
        var taskIds = notifications.Where(n => n.TaskId is not null).Select(n => n.TaskId!.Value).Distinct().ToList();
        var taskNames = await dbContext.TaskItems(taskIds).ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var items = notifications
            .Select(n => new NotificationItem(
                n.Id,
                n.Kind,
                n.Title,
                n.Message,
                n.ProjectId,
                projects.GetValueOrDefault(n.ProjectId, string.Empty),
                n.WorkPackageId,
                n.TaskId,
                n.TaskId is { } taskId ? taskNames.GetValueOrDefault(taskId) : null,
                n.RecipientEmployeeId is null,
                n.CreatedAt,
                n.ReadAt is not null))
            .ToList();

        return new NotificationList(items, unreadCount);
    }
}
