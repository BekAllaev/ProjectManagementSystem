using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.Specifications;

/// <summary>
/// The employee is a member of the project team on the given date.
/// </summary>
public sealed class IsTeamMemberOnSpecification(Guid employeeId, DateOnly date) : Specification<Project>
{
    public override bool IsSatisfiedBy(Project project) => project.IsTeamMemberOn(employeeId, date);
}
