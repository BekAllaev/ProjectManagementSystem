using ProjectManagementSystem.Application.Notifications;

namespace ProjectManagementSystem.Application.Abstractions;

/// <summary>
/// Sends notifications to people (e-mail, in-app). Implemented by the infrastructure.
/// Domain events are delivered at least once, so an implementation must ignore a notification whose
/// <see cref="Notification.IdempotencyKey"/> it has already sent.
/// </summary>
public interface INotificationSender
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}

/// <param name="IdempotencyKey">Same key means the same notification; a repeated one is not sent again.</param>
/// <param name="RecipientEmployeeId">
/// Empty means the management of the project; the domain has no role for it yet.
/// </param>
/// <param name="Title">Short headline shown in the list; <paramref name="Message"/> gives the details.</param>
/// <param name="WorkPackageId">Work package the notification links to, if any.</param>
/// <param name="TaskId">Task the notification links to, if any; its work package is <paramref name="WorkPackageId"/>.</param>
public sealed record Notification(
    string IdempotencyKey,
    Guid ProjectId,
    Guid? RecipientEmployeeId,
    NotificationKind Kind,
    string Title,
    string Message,
    Guid? WorkPackageId = null,
    Guid? TaskId = null);
