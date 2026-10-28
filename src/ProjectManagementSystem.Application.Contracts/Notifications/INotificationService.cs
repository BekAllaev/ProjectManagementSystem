using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Notifications.Commands.MarkAllNotificationsRead;
using ProjectManagementSystem.Application.Notifications.Commands.MarkNotificationRead;
using ProjectManagementSystem.Application.Notifications.Queries.GetNotifications;

namespace ProjectManagementSystem.Application.Notifications;

/// <summary>
/// gRPC service of the in-app notifications of the caller: one method per command and per query. The host implements
/// every method by sending the command or query through MediatR; the client shares this interface for typed calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.NotificationService")]
public interface INotificationService
{
    Task<NotificationList> GetNotifications(GetNotificationsQuery query, CallContext context = default);

    Task MarkAllNotificationsRead(MarkAllNotificationsReadCommand command, CallContext context = default);

    Task MarkNotificationRead(MarkNotificationReadCommand command, CallContext context = default);
}
