using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Projects;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Employees;

namespace ProjectManagementSystem.Application.Employees.Queries.GetEmployees;

/// <summary>
/// Employees have no events, so they are read from their aggregate; the projects they are on today come from the
/// membership periods of the read models.
/// </summary>
public sealed class GetEmployeesQueryHandler(
    IReadModelRepository<Employee> employees,
    IReadModelRepository<MembershipPeriodView> periods,
    IReadModelRepository<ProjectItemView> projects,
    TimeProvider timeProvider) : IQueryHandler<GetEmployeesQuery, EmployeeList>
{
    public async Task<EmployeeList> Handle(GetEmployeesQuery query, CancellationToken cancellationToken)
    {
        var text = query.Search?.Trim().ToLower() ?? string.Empty;
        var rows = await employees.ListAsync(
            e => text == string.Empty ||
                 e.FirstName.ToLower().Contains(text) || e.LastName.ToLower().Contains(text) || e.NormalizedEmail.Contains(text),
            cancellationToken);
        var inForce = await periods.ListOnAsync(timeProvider.Today(), cancellationToken: cancellationToken);
        var projectIds = inForce.Select(p => p.ProjectId).Distinct().ToList();
        var titles = (await projects.ListAsync(p => projectIds.Contains(p.Id), cancellationToken)).ToDictionary(p => p.Id, p => p.Title);
        var projectsOf = inForce.ToLookup(p => p.EmployeeId, p => p.ProjectId);

        return new EmployeeList(rows
            .OrderBy(e => e.LastName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(e => e.FirstName, StringComparer.CurrentCultureIgnoreCase)
            .Select(e => new EmployeeItem(
                e.Id, e.FirstName, e.LastName, e.Email, e.PhotoPath,
                projectsOf[e.Id]
                    .Distinct()
                    .Where(titles.ContainsKey)
                    .Select(id => new ProjectRef(id, titles[id]))
                    .OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
                e.Version))
            .ToList());
    }
}
