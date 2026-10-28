using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Notifications.Commands.MarkNotificationRead;
using ProjectManagementSystem.Infrastructure.Queries;
using ProjectManagementSystem.Infrastructure.Queries.Notifications;

namespace ProjectManagementSystem.Infrastructure.Persistence.Notifications;

/// <summary>
/// Notifications are records of the infrastructure, not aggregates, so this handler changes the row itself; the unit
/// of work saves it after the handler like any command. A notification the caller does not see is not found.
/// </summary>
internal sealed class MarkNotificationReadCommandHandler(AppDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task<Unit> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        var inbox = await dbContext.NotificationsOfAsync(currentUser.EmployeeId, timeProvider.Today(), cancellationToken);
        var notification = await inbox.SingleOrDefaultAsync(n => n.Id == command.NotificationId, cancellationToken)
            ?? throw new AggregateNotFoundException("Notification", command.NotificationId);

        notification.ReadAt ??= timeProvider.GetUtcNow().UtcDateTime;

        return Unit.Value;
    }
}
