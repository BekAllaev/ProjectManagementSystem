using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Specifications;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Operations on <see cref="Project"/> whose rules need its work packages, which live in their own
/// aggregates. The caller loads all work packages of the project by <see cref="Project.WorkPackageIds"/>;
/// an incomplete list is rejected, so a rule cannot be bypassed by leaving a package out.
/// Only <see cref="Project"/> is changed.
/// </summary>
public sealed class ProjectService(TimeProvider timeProvider)
{
    /// <summary>
    /// Completes the project when every registered work package is Completed or Cancelled. Work packages
    /// never reopen, and registering a package changes the version of <see cref="Project"/>, so the list
    /// cannot change concurrently. A package created but not yet registered is cancelled by the handler of
    /// <see cref="WorkPackages.Events.WorkPackageCreated"/> once it sees the closed project (compensation).
    /// </summary>
    public void Complete(Project project, IReadOnlyCollection<WorkPackage> workPackages)
    {
        WorkPackagesOfProject.EnsureComplete(project, workPackages);

        var unfinished = workPackages.FirstOrDefault(wp =>
            wp.Status is not (WorkPackageStatus.Completed or WorkPackageStatus.Cancelled));

        if (unfinished is not null)
            throw new DomainException(
                $"Project '{project.Id}' cannot be completed because work package '{unfinished.Id}' is {unfinished.Status}.");

        project.Complete(timeProvider.GetUtcNow().UtcDateTime);
    }

    /// <summary>
    /// Ends a team membership. An employee who is responsible for an open work package or task cannot
    /// leave the team.
    /// </summary>
    public void EndTeamMembership(
        Project project,
        IReadOnlyCollection<WorkPackage> workPackages,
        Guid employeeId,
        DateOnly to)
    {
        WorkPackagesOfProject.EnsureComplete(project, workPackages);

        var isResponsible = new IsResponsibleForOpenWorkSpecification(employeeId);

        if (workPackages.Any(wp => wp.ProjectId == project.Id && isResponsible.IsSatisfiedBy(wp)))
            throw new DomainException(
                $"Employee '{employeeId}' is responsible for an open work package or task and cannot leave the team.");

        project.EndTeamMembership(employeeId, to);
    }
}
