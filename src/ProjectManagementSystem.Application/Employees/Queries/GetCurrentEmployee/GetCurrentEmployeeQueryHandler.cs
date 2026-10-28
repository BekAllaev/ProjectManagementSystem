using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Domain.Employees;

namespace ProjectManagementSystem.Application.Employees.Queries.GetCurrentEmployee;

public sealed class GetCurrentEmployeeQueryHandler(IReadModelRepository<Employee> employees, ICurrentUser currentUser)
    : IQueryHandler<GetCurrentEmployeeQuery, CurrentEmployee>
{
    public async Task<CurrentEmployee> Handle(GetCurrentEmployeeQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.EmployeeId;
        var employee = await employees.FindAsync(e => e.Id == me, cancellationToken)
            ?? throw new AggregateNotFoundException(nameof(Employee), me);

        return new CurrentEmployee(employee.Id, employee.FirstName, employee.LastName, employee.Email, employee.PhotoPath, employee.Version);
    }
}
