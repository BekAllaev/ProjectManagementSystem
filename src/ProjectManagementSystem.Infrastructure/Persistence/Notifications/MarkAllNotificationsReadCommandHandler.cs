using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Notifications.Commands.MarkAllNotificationsRead;
using ProjectManagementSystem.Infrastructure.Queries;
using ProjectManagementSystem.Infrastructure.Queries.Notifications;

namespace ProjectManagementSystem.Infrastructure.Persistence.Notifications;

/// <summary>
/// Changes the rows themselves, like <see cref="MarkNotificationReadCommandHandler"/>; the unit of work saves them.
/// </summary>
internal sealed class MarkAllNotificationsReadCommandHandler(AppDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<MarkAllNotificationsReadCommand>
{
    public async Task<Unit> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        var inbox = await dbContext.NotificationsOfAsync(currentUser.EmployeeId, timeProvider.Today(), cancellationToken);
        var unread = await inbox.Where(n => n.ReadAt == null).ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var notification in unread)
            notification.ReadAt = now;

        return Unit.Value;
    }
}
