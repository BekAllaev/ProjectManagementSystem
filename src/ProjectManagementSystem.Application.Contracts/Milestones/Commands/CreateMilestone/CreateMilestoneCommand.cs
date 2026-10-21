using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.CreateMilestone;

/// <returns>Id of the new milestone.</returns>
public sealed record CreateMilestoneCommand(Guid ProjectId, string Name, string? Description, DateOnly TargetDate)
    : ICommand<Guid>;
