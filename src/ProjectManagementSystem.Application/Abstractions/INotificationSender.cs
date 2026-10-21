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
public sealed record Notification(string IdempotencyKey, Guid ProjectId, Guid? RecipientEmployeeId, string Message);
