using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Notifications.Queries.GetNotifications;

/// <summary>
/// Notifications of the caller, newest first: those addressed to them and those for the management of the projects
/// whose team they are a member of.
/// </summary>
/// <param name="Take">At most this many notifications; empty for all.</param>
[DataContract]
public sealed record GetNotificationsQuery(
    [property: DataMember(Order = 1)] bool UnreadOnly,
    [property: DataMember(Order = 2)] int? Take) : IQuery<NotificationList>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetNotificationsQuery() : this(default, default)
    {
    }
}
