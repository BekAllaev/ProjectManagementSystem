using ProjectManagementSystem.Domain.Projects.Events;

namespace ProjectManagementSystem.Domain.Projects;

public sealed partial class Project
{
    // A list, not a set: EF Core stores only arrays and lists as a primitive collection. RegisterWorkPackage keeps the ids unique.
    private readonly List<Guid> _workPackageIds = [];

    /// <summary>
    /// Ids of all registered work packages of the project, finished ones included. A new work package is
    /// registered after its creation is committed (eventual consistency), so for a short time it can be
    /// missing here. Changing the list changes the version of the project like any other change inside the
    /// aggregate, so registering a work package and closing the project conflict with each other.
    /// </summary>
    public IReadOnlyCollection<Guid> WorkPackageIds => _workPackageIds.AsReadOnly();

    /// <summary>
    /// Called by the handler of <see cref="WorkPackages.Events.WorkPackageCreated"/>. No work package can be
    /// registered in a closed project; the handler cancels such a package instead. Registering the same
    /// package again changes nothing, so a repeated delivery of the event is harmless.
    /// </summary>
    public void RegisterWorkPackage(Guid workPackageId)
    {
        if (_workPackageIds.Contains(workPackageId))
            return;

        EnsureOpen("add a work package to");

        _workPackageIds.Add(workPackageId);
        RaiseDomainEvent(new WorkPackageRegistered(Id, workPackageId));
    }
}
