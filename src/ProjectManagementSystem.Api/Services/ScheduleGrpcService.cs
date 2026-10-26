using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Application.Schedule;
using ProjectManagementSystem.Application.Schedule.Commands.AddDependency;
using ProjectManagementSystem.Application.Schedule.Commands.RemoveDependency;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Services;

/// <summary>
/// Sends every command of <see cref="IScheduleService"/> through MediatR, so the pipeline behaviors apply. No logic here.
/// </summary>
[Authorize]
public sealed class ScheduleGrpcService(ISender mediator) : IScheduleService
{
    public Task AddDependency(AddDependencyCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task RemoveDependency(RemoveDependencyCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);
}
