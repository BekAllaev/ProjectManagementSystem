using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.CloseProject;

/// <summary>
/// Completes the project; every registered work package must be finished.
/// </summary>
public sealed record CloseProjectCommand(Guid ProjectId) : ICommand;
