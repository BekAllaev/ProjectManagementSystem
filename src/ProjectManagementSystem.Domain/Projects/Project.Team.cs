using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Projects.Team;

namespace ProjectManagementSystem.Domain.Projects;

public sealed partial class Project
{
    private readonly List<TeamMember> _teamMembers = [];

    public IReadOnlyCollection<TeamMember> TeamMembers => _teamMembers.AsReadOnly();

    /// <summary>
    /// Adds an employee to the team. The first hourly rate is required and starts at <paramref name="from"/>.
    /// </summary>
    public Guid AddTeamMember(Guid employeeId, Guid roleId, DateOnly from, decimal hourlyRate, DateOnly? to = null)
    {
        GetRole(roleId);

        if (_teamMembers.Any(m => m.EmployeeId == employeeId && m.Overlaps(from, to)))
            throw new DomainException("Periods of the same employee on the project must not overlap.");

        var teamMember = new TeamMember(Guid.NewGuid(), employeeId, roleId, from, to, hourlyRate);
        _teamMembers.Add(teamMember);

        RaiseDomainEvent(new TeamMemberAdded(Id, teamMember.Id, employeeId, roleId, from, to, hourlyRate));
        return teamMember.Id;
    }

    public void EndTeamMembership(Guid employeeId, DateOnly to)
    {
        var teamMember = _teamMembers.SingleOrDefault(m => m.EmployeeId == employeeId && m.To is null)
            ?? throw new DomainException($"Employee '{employeeId}' has no open team membership in the project.");

        teamMember.End(to);
        RaiseDomainEvent(new TeamMembershipEnded(Id, teamMember.Id, employeeId, to));
    }

    /// <summary>
    /// Adds a new rate to the chain of rates of the membership that covers <paramref name="from"/>.
    /// </summary>
    public void ChangeHourlyRate(Guid employeeId, decimal amount, DateOnly from)
    {
        var teamMember = _teamMembers.SingleOrDefault(m => m.EmployeeId == employeeId && m.IsActiveOn(from))
            ?? throw new DomainException($"Employee '{employeeId}' is not on the project on {from}.");

        var rate = teamMember.ChangeHourlyRate(amount, from, DateOnly.FromDateTime(DateTime.UtcNow));
        RaiseDomainEvent(new HourlyRateChanged(Id, teamMember.Id, employeeId, rate.Amount, rate.From));
    }

    public HourlyRate GetHourlyRateOn(Guid employeeId, DateOnly date)
    {
        var teamMember = _teamMembers.SingleOrDefault(m => m.EmployeeId == employeeId && m.IsActiveOn(date))
            ?? throw new DomainException($"Employee '{employeeId}' is not on the project on {date}.");

        return teamMember.GetHourlyRateOn(date);
    }

    public bool IsTeamMemberOn(Guid employeeId, DateOnly date) =>
        _teamMembers.Any(m => m.EmployeeId == employeeId && m.IsActiveOn(date));

    // History counts too: a role stays "assigned" even after the membership has ended.
    private bool IsRoleEverAssigned(Guid roleId) => _teamMembers.Any(m => m.RoleId == roleId);
}
