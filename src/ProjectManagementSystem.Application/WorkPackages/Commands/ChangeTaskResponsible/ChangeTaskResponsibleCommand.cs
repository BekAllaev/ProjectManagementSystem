using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.ChangeTaskResponsible;

public sealed record ChangeTaskResponsibleCommand(Guid WorkPackageId, Guid TaskId, Guid EmployeeId) : ICommand;
