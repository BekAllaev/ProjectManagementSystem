using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Employees;

namespace ProjectManagementSystem.Application.Projects.Queries.GetTeam;

/// <summary>
/// Who is a member today and which rate is valid today depend on the day, so they are taken from the stored periods
/// and rates on every read; the names come from the employees, which have no events.
/// </summary>
public sealed class GetTeamQueryHandler(
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<TeamMemberItemView> teamMembers,
    IReadModelRepository<MembershipPeriodView> periods,
    IReadModelRepository<HourlyRateItemView> rates,
    IReadModelRepository<Employee> employees,
    TimeProvider timeProvider) : IQueryHandler<GetTeamQuery, Team>
{
    public async Task<Team> Handle(GetTeamQuery query, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(query.ProjectId, cancellationToken);
        var members = await teamMembers.ListAsync(m => m.ProjectId == project.Id, cancellationToken);
        var periodsOf = (await periods.ListAsync(p => p.ProjectId == project.Id, cancellationToken)).ToLookup(p => p.EmployeeId);
        var ratesOf = (await rates.ListAsync(r => r.ProjectId == project.Id, cancellationToken)).ToLookup(r => r.EmployeeId);
        var names = await EmployeeDirectory.LoadAsync(employees, members.Select(m => m.EmployeeId), cancellationToken);
        var today = timeProvider.Today();

        var items = members
            .Select(m =>
            {
                var memberRates = ratesOf[m.EmployeeId].OrderByDescending(r => r.From).ToList();
                var current = memberRates.FirstOrDefault(r => r.From <= today) ?? memberRates.LastOrDefault();

                return new TeamMemberItem(
                    names.Ref(m.EmployeeId),
                    names.Email(m.EmployeeId),
                    m.RoleId,
                    m.RoleName,
                    periodsOf[m.EmployeeId].Any(p => p.From <= today && (p.To is null || today <= p.To)),
                    periodsOf[m.EmployeeId].OrderBy(p => p.From).Select(p => new MembershipPeriod(p.TeamMemberId, p.From, p.To)).ToList(),
                    current?.Amount ?? 0,
                    current?.From ?? default,
                    memberRates.Select(r => new HourlyRateItem(r.Amount, r.From)).ToList());
            })
            .OrderByDescending(m => m.IsActive)
            .ThenBy(m => names.SortName(m.Employee.Id), StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new Team(project.Id, items, project.Version);
    }
}
