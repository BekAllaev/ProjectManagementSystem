using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Employees.Commands.RegisterEmployee;

/// <returns>Id of the new employee.</returns>
public sealed record RegisterEmployeeCommand(string FirstName, string LastName, string Email, string? PhotoPath = null)
    : ICommand<Guid>;
