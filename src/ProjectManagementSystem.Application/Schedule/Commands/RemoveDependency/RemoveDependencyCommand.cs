using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Schedule.Commands.RemoveDependency;

public sealed record RemoveDependencyCommand(Guid ProjectId, Guid PredecessorTaskId, Guid SuccessorTaskId) : ICommand;
