using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.TimeTracking;
using ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;
using ProjectManagementSystem.Application.TimeTracking.Commands.RemoveTimeEntry;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Services;

/// <summary>
/// Sends every command of <see cref="ITimeTrackingService"/> through MediatR, so the pipeline behaviors apply. No logic here.
/// </summary>
[Authorize]
public sealed class TimeTrackingGrpcService(ISender mediator) : ITimeTrackingService
{
    public Task<CreatedId> LogTime(LogTimeCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task RemoveTimeEntry(RemoveTimeEntryCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);
}
