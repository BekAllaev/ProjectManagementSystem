using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Employees.Queries.GetEmployees;
using ProjectManagementSystem.Application.Projects;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Employees;

internal sealed class GetEmployeesQueryHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetEmployeesQuery, EmployeeList>
{
    public async Task<EmployeeList> Handle(GetEmployeesQuery query, CancellationToken cancellationToken)
    {
        var employees = await dbContext.Employees(query.Search).ToListAsync(cancellationToken);
        var memberships = await dbContext.MembershipsOn(timeProvider.Today()).ToListAsync(cancellationToken);
        var projectsByEmployee = memberships.ToLookup(m => m.EmployeeId);

        var items = employees
            .OrderBy(e => e.LastName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(e => e.FirstName, StringComparer.CurrentCultureIgnoreCase)
            .Select(e => new EmployeeItem(
                e.Id, e.FirstName, e.LastName, e.Email, e.PhotoPath,
                projectsByEmployee[e.Id]
                    .DistinctBy(m => m.ProjectId)
                    .OrderBy(m => m.ProjectTitle, StringComparer.CurrentCultureIgnoreCase)
                    .Select(m => new ProjectRef(m.ProjectId, m.ProjectTitle))
                    .ToList(),
                e.Version))
            .ToList();

        return new EmployeeList(items);
    }
}
