using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.WorkPackages.Specifications;

/// <summary>
/// The employee is responsible for the work package while it is open, or for one of its unfinished tasks.
/// </summary>
public sealed class IsResponsibleForOpenWorkSpecification(Guid employeeId) : Specification<WorkPackage>
{
    public override bool IsSatisfiedBy(WorkPackage workPackage) =>
        workPackage.Status == WorkPackageStatus.Open
        && (workPackage.ResponsiblePersonId == employeeId
            || workPackage.Tasks.Any(t => !t.IsFinished && t.ResponsiblePersonId == employeeId));
}
