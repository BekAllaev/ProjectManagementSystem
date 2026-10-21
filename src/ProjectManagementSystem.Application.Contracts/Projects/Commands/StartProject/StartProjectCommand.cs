using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.StartProject;

public sealed record StartProjectCommand(Guid ProjectId) : ICommand;
