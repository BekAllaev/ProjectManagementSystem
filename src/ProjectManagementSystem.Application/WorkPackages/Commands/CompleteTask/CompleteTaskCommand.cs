using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;

public sealed record CompleteTaskCommand(Guid WorkPackageId, Guid TaskId) : ICommand;
