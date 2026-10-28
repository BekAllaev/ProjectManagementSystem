using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Notifications;

namespace ProjectManagementSystem.Application.Notifications.Queries.GetNotifications;

/// <param name="UnreadCount">All unread notifications of the caller, regardless of the filters.</param>
[DataContract]
public sealed record NotificationList(
    [property: DataMember(Order = 1)] IReadOnlyList<NotificationItem> Items,
    [property: DataMember(Order = 2)] int UnreadCount)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private NotificationList() : this(new List<NotificationItem>(), default)
    {
    }
}

/// <param name="TaskName">Name of <paramref name="TaskId"/>, if the notification links to a task.</param>
/// <param name="IsForProjectManagement">
/// Addressed to the management of the project rather than to the caller; read by one member, it is read for all.
/// </param>
/// <param name="CreatedAt">In UTC.</param>
[DataContract]
public sealed record NotificationItem(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] NotificationKind Kind,
    [property: DataMember(Order = 3)] string Title,
    [property: DataMember(Order = 4)] string Message,
    [property: DataMember(Order = 5)] Guid ProjectId,
    [property: DataMember(Order = 6)] string ProjectTitle,
    [property: DataMember(Order = 7)] Guid? WorkPackageId,
    [property: DataMember(Order = 8)] Guid? TaskId,
    [property: DataMember(Order = 9)] string? TaskName,
    [property: DataMember(Order = 10)] bool IsForProjectManagement,
    [property: DataMember(Order = 11)] DateTime CreatedAt,
    [property: DataMember(Order = 12)] bool IsRead)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private NotificationItem() : this(
        default, default, default!, default!, default, default!, default, default, default, default, default, default)
    {
    }
}
