namespace ProjectManagementSystem.Domain.Projects;

public sealed partial class Project
{
    private readonly HashSet<Guid> _workPackageIds = [];

    /// <summary>
    /// Ids of all work packages of the project, finished ones included. Changing the list changes the
    /// version of the project like any other change inside the aggregate, so creating a work package and
    /// closing the project conflict with each other.
    /// </summary>
    public IReadOnlyCollection<Guid> WorkPackageIds => _workPackageIds;

    /// <summary>
    /// Goes through <see cref="Services.WorkPackageService.CreateWorkPackage"/>, which creates the
    /// work package; no new work package can appear in a closed project.
    /// </summary>
    internal void RegisterWorkPackage(Guid workPackageId)
    {
        EnsureOpen("add a work package to");

        _workPackageIds.Add(workPackageId);
    }
}
