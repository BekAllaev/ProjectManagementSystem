using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Projects;

internal static class WorkPackagesOfProject
{
    /// <summary>
    /// The caller loads the work packages by <see cref="Project.WorkPackageIds"/>; this checks that the set
    /// is exactly that list, with no package missing and no foreign one.
    /// </summary>
    public static void EnsureComplete(Project project, IReadOnlyCollection<WorkPackage> workPackages)
    {
        if (!project.WorkPackageIds.ToHashSet().SetEquals(workPackages.Select(wp => wp.Id)))
            throw new DomainException($"The given work packages are not exactly the work packages of project '{project.Id}'.");
    }
}
