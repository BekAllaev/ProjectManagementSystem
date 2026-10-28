using ProjectManagementSystem.Application.Notifications;

namespace ProjectManagementSystem.Infrastructure.Persistence.Notifications;

/// <summary>
/// In-app notification. <see cref="IdempotencyKey"/> is unique, so a repeated delivery of an event does not
/// notify twice.
/// </summary>
public sealed class NotificationRecord
{
    public Guid Id { get; init; }

    public required string IdempotencyKey { get; init; }

    public Guid ProjectId { get; init; }

    /// <summary>
    /// Empty when the notification is for the management of the project.
    /// </summary>
    public Guid? RecipientEmployeeId { get; init; }

    public NotificationKind Kind { get; init; }

    public required string Title { get; init; }

    public required string Message { get; init; }

    /// <summary>
    /// Work package the notification links to, if any.
    /// </summary>
    public Guid? WorkPackageId { get; init; }

    /// <summary>
    /// Task the notification links to, if any; it is in <see cref="WorkPackageId"/>.
    /// </summary>
    public Guid? TaskId { get; init; }

    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Empty until the recipient reads the notification.
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
