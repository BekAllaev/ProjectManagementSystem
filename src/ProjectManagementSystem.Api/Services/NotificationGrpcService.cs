using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Application.Notifications;
using ProjectManagementSystem.Application.Notifications.Commands.MarkAllNotificationsRead;
using ProjectManagementSystem.Application.Notifications.Commands.MarkNotificationRead;
using ProjectManagementSystem.Application.Notifications.Queries.GetNotifications;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Services;

/// <summary>
/// Sends every command and query of <see cref="INotificationService"/> through MediatR, so the pipeline behaviors apply.
/// No logic here.
/// </summary>
[Authorize]
public sealed class NotificationGrpcService(ISender mediator) : INotificationService
{
    public Task<NotificationList> GetNotifications(GetNotificationsQuery query, CallContext context = default) =>
        mediator.Send(query, context.CancellationToken);

    public Task MarkAllNotificationsRead(MarkAllNotificationsReadCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task MarkNotificationRead(MarkNotificationReadCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);
}
