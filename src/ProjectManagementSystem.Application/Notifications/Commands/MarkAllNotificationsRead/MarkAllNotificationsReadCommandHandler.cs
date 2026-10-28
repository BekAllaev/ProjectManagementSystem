using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.ReadModels;

namespace ProjectManagementSystem.Application.Notifications.Commands.MarkAllNotificationsRead;

/// <summary>
/// Changes the rows themselves, like <see cref="MarkNotificationRead.MarkNotificationReadCommandHandler"/>; the unit
/// of work saves them.
/// </summary>
public sealed class MarkAllNotificationsReadCommandHandler(
    IReadModelRepository<NotificationItemView> notifications,
    IReadModelRepository<MembershipPeriodView> periods,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<MarkAllNotificationsReadCommand>
{
    public async Task<Unit> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        var inbox = await NotificationInbox.ListAsync(notifications, periods, currentUser.EmployeeId, timeProvider.Today(), cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var notification in inbox.Where(n => n.ReadAt is null))
            notification.ReadAt = now;

        return Unit.Value;
    }
}
