using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Projects.Queries.GetTeam;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Projects;

/// <summary>
/// Who is a member today and which rate is valid today depend on the day, so they are taken from the stored periods
/// and rates on every read; the names come from the employees, which have no events.
/// </summary>
internal sealed class GetTeamQueryHandler(AppDbContext dbContext, TimeProvider timeProvider) : IQueryHandler<GetTeamQuery, Team>
{
    public async Task<Team> Handle(GetTeamQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectItemAsync(query.ProjectId, cancellationToken);
        var members = await dbContext.TeamMemberItemsOf(project.Id).ToListAsync(cancellationToken);
        var periods = (await dbContext.MembershipPeriodsOf(project.Id).ToListAsync(cancellationToken)).ToLookup(p => p.EmployeeId);
        var rates = (await dbContext.HourlyRatesOf(project.Id).ToListAsync(cancellationToken)).ToLookup(r => r.EmployeeId);
        var employees = await EmployeeDirectory.LoadAsync(dbContext, members.Select(m => m.EmployeeId), cancellationToken);
        var today = timeProvider.Today();

        var items = members
            .Select(m =>
            {
                var memberRates = rates[m.EmployeeId].OrderByDescending(r => r.From).ToList();
                var current = memberRates.FirstOrDefault(r => r.From <= today) ?? memberRates.LastOrDefault();

                return new TeamMemberItem(
                    employees.Ref(m.EmployeeId),
                    employees.Email(m.EmployeeId),
                    m.RoleId,
                    m.RoleName,
                    periods[m.EmployeeId].Any(p => p.From <= today && (p.To is null || today <= p.To)),
                    periods[m.EmployeeId].OrderBy(p => p.From).Select(p => new MembershipPeriod(p.TeamMemberId, p.From, p.To)).ToList(),
                    current?.Amount ?? 0,
                    current?.From ?? default,
                    memberRates.Select(r => new HourlyRateItem(r.Amount, r.From)).ToList());
            })
            .OrderByDescending(m => m.IsActive)
            .ThenBy(m => employees.SortName(m.Employee.Id), StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new Team(project.Id, items, project.Version);
    }
}
