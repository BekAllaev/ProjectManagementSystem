using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Employees.Commands.RenameEmployee;

public sealed record RenameEmployeeCommand(Guid EmployeeId, string FirstName, string LastName) : ICommand;
