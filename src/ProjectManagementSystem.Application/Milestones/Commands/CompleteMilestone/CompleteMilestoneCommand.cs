using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;

/// <summary>
/// Completes the milestone, even if not all its tasks are done; afterwards it is frozen.
/// </summary>
public sealed record CompleteMilestoneCommand(Guid MilestoneId) : ICommand;
