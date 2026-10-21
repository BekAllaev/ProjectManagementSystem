using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;

public sealed record AddTaskToMilestoneCommand(Guid MilestoneId, Guid WorkPackageId, Guid TaskId) : ICommand;
