using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.ReadModels;

namespace ProjectManagementSystem.Application.Notifications.Commands.MarkNotificationRead;

/// <summary>
/// Notifications are read models, not aggregates, so this handler changes the row itself; the unit of work saves it
/// after the handler like any command. A notification the caller does not see is not found.
/// </summary>
public sealed class MarkNotificationReadCommandHandler(
    IReadModelRepository<NotificationItemView> notifications,
    IReadModelRepository<MembershipPeriodView> periods,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task<Unit> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        var inbox = await NotificationInbox.ListAsync(notifications, periods, currentUser.EmployeeId, timeProvider.Today(), cancellationToken);
        var notification = inbox.SingleOrDefault(n => n.Id == command.NotificationId)
            ?? throw new AggregateNotFoundException("Notification", command.NotificationId);

        notification.ReadAt ??= timeProvider.GetUtcNow().UtcDateTime;

        return Unit.Value;
    }
}
