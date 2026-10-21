using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Services;

namespace ProjectManagementSystem.Application.Employees.Commands.RegisterEmployee;

/// <summary>
/// The domain service rejects an email already used by another employee.
/// </summary>
public sealed class RegisterEmployeeCommandHandler(IRepository<Employee> employees, EmployeeService employeeService)
    : ICommandHandler<RegisterEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(RegisterEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await employeeService.RegisterAsync(
            command.FirstName, command.LastName, command.Email, command.PhotoPath, cancellationToken);

        employees.Add(employee);
        return employee.Id;
    }
}
