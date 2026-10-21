using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Application.Employees.Commands.RenameEmployee;

public sealed class RenameEmployeeCommandHandler(IRepository<Employee> employees)
    : ICommandHandler<RenameEmployeeCommand>
{
    public async Task<Unit> Handle(RenameEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await employees.GetAsync(command.EmployeeId, cancellationToken);

        employee.Rename(command.FirstName, command.LastName);

        employees.Update(employee);
        return Unit.Value;
    }
}
