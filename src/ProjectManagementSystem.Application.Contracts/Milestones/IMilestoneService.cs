using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.ChangeMilestoneTargetDate;
using ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.CreateMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.DeleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.RemoveTaskFromMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.RenameMilestone;

namespace ProjectManagementSystem.Application.Milestones;

/// <summary>
/// gRPC service of the Milestone aggregate: one method per command. The host implements every
/// method by sending the command through MediatR; the client shares this interface for typed calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.MilestoneService")]
public interface IMilestoneService
{
    Task AddTaskToMilestone(AddTaskToMilestoneCommand command, CallContext context = default);

    Task ChangeMilestoneTargetDate(ChangeMilestoneTargetDateCommand command, CallContext context = default);

    Task CompleteMilestone(CompleteMilestoneCommand command, CallContext context = default);

    Task<CreatedId> CreateMilestone(CreateMilestoneCommand command, CallContext context = default);

    Task DeleteMilestone(DeleteMilestoneCommand command, CallContext context = default);

    Task RemoveTaskFromMilestone(RemoveTaskFromMilestoneCommand command, CallContext context = default);

    Task RenameMilestone(RenameMilestoneCommand command, CallContext context = default);
}
