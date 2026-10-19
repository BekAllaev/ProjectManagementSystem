using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Projects.Team;

namespace ProjectManagementSystem.Domain.Projects;

public sealed partial class Project
{
    private readonly List<TeamMember> _teamMembers = [];

    public IReadOnlyCollection<TeamMember> TeamMembers => _teamMembers.AsReadOnly();

    public Guid AddTeamMember(Guid employeeId, Guid roleId, DateOnly from, DateOnly? to = null)
    {
        GetRole(roleId);

        if (_teamMembers.Any(m => m.EmployeeId == employeeId && m.Overlaps(from, to)))
            throw new DomainException("Periods of the same employee on the project must not overlap.");

        var teamMember = new TeamMember(Guid.NewGuid(), employeeId, roleId, from, to);
        _teamMembers.Add(teamMember);

        RaiseDomainEvent(new TeamMemberAdded(Id, teamMember.Id, employeeId, roleId, from, to));
        return teamMember.Id;
    }

    public void EndTeamMembership(Guid employeeId, DateOnly to)
    {
        var teamMember = _teamMembers.SingleOrDefault(m => m.EmployeeId == employeeId && m.To is null)
            ?? throw new DomainException($"Employee '{employeeId}' has no open team membership in the project.");

        teamMember.End(to);
        RaiseDomainEvent(new TeamMembershipEnded(Id, teamMember.Id, employeeId, to));
    }

    public bool IsTeamMemberOn(Guid employeeId, DateOnly date) =>
        _teamMembers.Any(m => m.EmployeeId == employeeId && m.IsActiveOn(date));

    // History counts too: a role stays "assigned" even after the membership has ended.
    private bool IsRoleEverAssigned(Guid roleId) => _teamMembers.Any(m => m.RoleId == roleId);
}
