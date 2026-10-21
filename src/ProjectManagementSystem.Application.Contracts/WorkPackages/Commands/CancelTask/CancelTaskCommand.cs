using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;

public sealed record CancelTaskCommand(Guid WorkPackageId, Guid TaskId) : ICommand;
