using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Employees.Queries.GetCurrentEmployee;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Employees;

internal sealed class GetCurrentEmployeeQueryHandler(AppDbContext dbContext, ICurrentUser currentUser)
    : IQueryHandler<GetCurrentEmployeeQuery, CurrentEmployee>
{
    public async Task<CurrentEmployee> Handle(GetCurrentEmployeeQuery query, CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees([currentUser.EmployeeId]).SingleOrDefaultAsync(cancellationToken)
            ?? throw new AggregateNotFoundException(nameof(Employee), currentUser.EmployeeId);

        return new CurrentEmployee(employee.Id, employee.FirstName, employee.LastName, employee.Email, employee.PhotoPath, employee.Version);
    }
}
