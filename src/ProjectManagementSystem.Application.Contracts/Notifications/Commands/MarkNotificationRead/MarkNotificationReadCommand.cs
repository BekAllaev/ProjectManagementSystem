using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Notifications.Commands.MarkNotificationRead;

/// <summary>
/// Marks a notification of the caller as read; a read one stays as it is.
/// </summary>
[DataContract]
public sealed record MarkNotificationReadCommand(
    [property: DataMember(Order = 1)] Guid NotificationId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private MarkNotificationReadCommand() : this(Guid.Empty)
    {
    }
}
