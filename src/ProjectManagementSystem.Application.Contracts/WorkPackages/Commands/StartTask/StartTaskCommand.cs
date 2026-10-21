using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;

public sealed record StartTaskCommand(Guid WorkPackageId, Guid TaskId) : ICommand;
