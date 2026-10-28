using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;

namespace ProjectManagementSystem.Infrastructure.Persistence.Notifications;

/// <summary>
/// In-app notifications. Adds a row to the current <see cref="AppDbContext"/> without saving it: the outbox processor
/// saves it together with the mark that the event was processed, so a handler that only notifies is atomic. A
/// notification whose key is already stored, or was already added in this scope, is ignored: events are delivered at
/// least once. The unique index on the key backs this up.
/// </summary>
internal sealed class NotificationSender(AppDbContext dbContext, TimeProvider timeProvider) : INotificationSender
{
    public async Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var key = notification.IdempotencyKey;

        if (dbContext.Notifications.Local.Any(n => n.IdempotencyKey == key) ||
            await dbContext.Notifications.AnyAsync(n => n.IdempotencyKey == key, cancellationToken))
            return;

        var now = timeProvider.GetUtcNow();

        dbContext.Notifications.Add(new NotificationRecord
        {
            Id = Guid.CreateVersion7(now),
            IdempotencyKey = key,
            ProjectId = notification.ProjectId,
            RecipientEmployeeId = notification.RecipientEmployeeId,
            Kind = notification.Kind,
            Title = notification.Title,
            Message = notification.Message,
            WorkPackageId = notification.WorkPackageId,
            TaskId = notification.TaskId,
            CreatedAt = now.UtcDateTime,
        });
    }
}
