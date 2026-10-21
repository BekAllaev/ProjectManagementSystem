using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CompleteWorkPackage;

public sealed record CompleteWorkPackageCommand(Guid WorkPackageId) : ICommand;
