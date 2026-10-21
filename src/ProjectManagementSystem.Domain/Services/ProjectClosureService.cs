using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Completes a project. Every work package of the project must be Completed or Cancelled; work packages
/// live in their own aggregates, so the rule spans aggregates. Work packages never reopen, and the list of
/// packages cannot change concurrently because registering a package changes the version of
/// <see cref="Project"/>, so the rule holds at commit and there is no re-check after it.
/// Only <see cref="Project"/> is changed.
/// </summary>
public sealed class ProjectClosureService(TimeProvider timeProvider)
{
    /// <param name="workPackages">
    /// All work packages of the project, loaded by <see cref="Project.WorkPackageIds"/>. An incomplete
    /// list is rejected, so the project cannot be closed by leaving an unfinished package out.
    /// </param>
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
}
