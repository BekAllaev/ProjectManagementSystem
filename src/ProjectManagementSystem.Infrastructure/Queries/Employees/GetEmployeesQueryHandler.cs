using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Employees.Queries.GetEmployees;
using ProjectManagementSystem.Application.Projects;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Employees;

/// <summary>
/// Employees have no events, so they are read from their aggregate; the projects they are on today come from the
/// membership periods of the read models.
/// </summary>
internal sealed class GetEmployeesQueryHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetEmployeesQuery, EmployeeList>
{
    public async Task<EmployeeList> Handle(GetEmployeesQuery query, CancellationToken cancellationToken)
    {
        var employees = await dbContext.Employees(query.Search).ToListAsync(cancellationToken);
        var periods = await dbContext.MembershipPeriodsOn(timeProvider.Today()).ToListAsync(cancellationToken);
        var titles = await dbContext.ProjectItems(periods.Select(p => p.ProjectId).Distinct().ToList())
            .ToDictionaryAsync(p => p.Id, p => p.Title, cancellationToken);
        var projectsByEmployee = periods.ToLookup(p => p.EmployeeId, p => p.ProjectId);

        return new EmployeeList(employees
            .OrderBy(e => e.LastName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(e => e.FirstName, StringComparer.CurrentCultureIgnoreCase)
            .Select(e => new EmployeeItem(
                e.Id, e.FirstName, e.LastName, e.Email, e.PhotoPath,
                projectsByEmployee[e.Id]
                    .Distinct()
                    .Where(titles.ContainsKey)
                    .Select(id => new ProjectRef(id, titles[id]))
                    .OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
                e.Version))
            .ToList());
    }
}
