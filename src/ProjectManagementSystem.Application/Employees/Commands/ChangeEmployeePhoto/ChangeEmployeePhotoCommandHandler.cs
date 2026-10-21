using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeePhoto;

public sealed class ChangeEmployeePhotoCommandHandler(IRepository<Employee> employees)
    : ICommandHandler<ChangeEmployeePhotoCommand>
{
    public async Task<Unit> Handle(ChangeEmployeePhotoCommand command, CancellationToken cancellationToken)
    {
        var employee = await employees.GetAsync(command.EmployeeId, cancellationToken);

        employee.ChangePhoto(command.PhotoPath);

        employees.Update(employee);
        return Unit.Value;
    }
}
