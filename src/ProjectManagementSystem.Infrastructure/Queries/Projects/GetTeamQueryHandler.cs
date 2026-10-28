using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Projects.Queries.GetTeam;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Projects;

/// <summary>
/// The domain keeps one team member per membership period; the team shows one item per employee with all periods.
/// </summary>
internal sealed class GetTeamQueryHandler(AppDbContext dbContext, TimeProvider timeProvider) : IQueryHandler<GetTeamQuery, Team>
{
    public async Task<Team> Handle(GetTeamQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectAsync(query.ProjectId, cancellationToken);
        var employees = await EmployeeDirectory.LoadAsync(dbContext, project.TeamMembers.Select(m => m.EmployeeId), cancellationToken);
        var roles = project.Roles.ToDictionary(r => r.Id, r => r.Name);
        var today = timeProvider.Today();

        var members = project.TeamMembers
            .GroupBy(m => m.EmployeeId)
            .Select(memberships =>
            {
                var periods = memberships.OrderBy(m => m.From).ToList();
                var latest = periods[^1];
                var rates = periods.SelectMany(m => m.HourlyRates).OrderByDescending(r => r.From).ToList();
                var current = rates.FirstOrDefault(r => r.From <= today) ?? rates[^1];

                return new TeamMemberItem(
                    employees.Ref(memberships.Key),
                    employees.Email(memberships.Key),
                    latest.RoleId,
                    roles.GetValueOrDefault(latest.RoleId, string.Empty),
                    periods.Any(m => m.IsActiveOn(today)),
                    periods.Select(m => new MembershipPeriod(m.Id, m.From, m.To)).ToList(),
                    current.Amount,
                    current.From,
                    rates.Select(r => new HourlyRateItem(r.Amount, r.From)).ToList());
            })
            .OrderByDescending(m => m.IsActive)
            .ThenBy(m => employees.SortName(m.Employee.Id), StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new Team(project.Id, members, project.Version);
    }
}
