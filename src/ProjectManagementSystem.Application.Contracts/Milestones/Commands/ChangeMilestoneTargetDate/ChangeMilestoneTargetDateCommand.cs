using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.ChangeMilestoneTargetDate;

public sealed record ChangeMilestoneTargetDateCommand(Guid MilestoneId, DateOnly TargetDate) : ICommand;
