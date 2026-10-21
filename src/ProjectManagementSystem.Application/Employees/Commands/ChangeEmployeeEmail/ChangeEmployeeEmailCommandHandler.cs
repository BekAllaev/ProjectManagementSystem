using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Services;

namespace ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeeEmail;

public sealed class ChangeEmployeeEmailCommandHandler(IRepository<Employee> employees, EmployeeService employeeService)
    : ICommandHandler<ChangeEmployeeEmailCommand>
{
    public async Task<Unit> Handle(ChangeEmployeeEmailCommand command, CancellationToken cancellationToken)
    {
        var employee = await employees.GetAsync(command.EmployeeId, cancellationToken);

        await employeeService.ChangeEmailAsync(employee, command.Email, cancellationToken);

        employees.Update(employee);
        return Unit.Value;
    }
}
