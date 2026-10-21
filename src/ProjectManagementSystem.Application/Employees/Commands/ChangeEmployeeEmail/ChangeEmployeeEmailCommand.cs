using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeeEmail;

public sealed record ChangeEmployeeEmailCommand(Guid EmployeeId, string Email) : ICommand;
