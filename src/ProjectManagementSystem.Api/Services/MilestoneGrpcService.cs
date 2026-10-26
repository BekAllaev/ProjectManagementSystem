using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Milestones;
using ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.ChangeMilestoneTargetDate;
using ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.CreateMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.DeleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.RemoveTaskFromMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.RenameMilestone;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Services;

/// <summary>
/// Sends every command of <see cref="IMilestoneService"/> through MediatR, so the pipeline behaviors apply. No logic here.
/// </summary>
[Authorize]
public sealed class MilestoneGrpcService(ISender mediator) : IMilestoneService
{
    public Task AddTaskToMilestone(AddTaskToMilestoneCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task ChangeMilestoneTargetDate(ChangeMilestoneTargetDateCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task CompleteMilestone(CompleteMilestoneCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task<CreatedId> CreateMilestone(CreateMilestoneCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task DeleteMilestone(DeleteMilestoneCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task RemoveTaskFromMilestone(RemoveTaskFromMilestoneCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task RenameMilestone(RenameMilestoneCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);
}
