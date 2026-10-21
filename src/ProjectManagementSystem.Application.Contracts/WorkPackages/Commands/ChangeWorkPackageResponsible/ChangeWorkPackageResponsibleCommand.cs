using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.ChangeWorkPackageResponsible;

public sealed record ChangeWorkPackageResponsibleCommand(Guid WorkPackageId, Guid EmployeeId) : ICommand;
