using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.RemoveTaskFromMilestone;

public sealed record RemoveTaskFromMilestoneCommand(Guid MilestoneId, Guid TaskId) : ICommand;
