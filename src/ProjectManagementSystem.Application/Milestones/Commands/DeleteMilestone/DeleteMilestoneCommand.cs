using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.DeleteMilestone;

public sealed record DeleteMilestoneCommand(Guid MilestoneId) : ICommand;
