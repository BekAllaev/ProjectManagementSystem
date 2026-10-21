using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.RenameMilestone;

public sealed record RenameMilestoneCommand(Guid MilestoneId, string Name) : ICommand;
