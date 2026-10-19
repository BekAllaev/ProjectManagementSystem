using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Projects.WorkPackages;

namespace ProjectManagementSystem.Domain.Projects;

public sealed partial class Project
{
    private readonly List<WorkPackage> _workPackages = [];

    public IReadOnlyCollection<WorkPackage> WorkPackages => _workPackages.AsReadOnly();

    public Guid CreateWorkPackage(string description, Guid responsiblePersonId)
    {
        EnsureIsCurrentTeamMember(responsiblePersonId);

        var workPackage = new WorkPackage(Guid.NewGuid(), Id, description, responsiblePersonId);
        _workPackages.Add(workPackage);

        RaiseDomainEvent(new WorkPackageCreated(Id, workPackage.Id, workPackage.Description, responsiblePersonId));
        return workPackage.Id;
    }

    public void ChangeWorkPackageResponsiblePerson(Guid workPackageId, Guid employeeId)
    {
        var workPackage = GetWorkPackage(workPackageId);
        EnsureIsCurrentTeamMember(employeeId);

        workPackage.ChangeResponsiblePerson(employeeId);
        RaiseDomainEvent(new WorkPackageResponsiblePersonChanged(Id, workPackage.Id, employeeId));
    }

    public Guid AddTask(
        Guid workPackageId,
        string name,
        string description,
        Guid responsiblePersonId,
        decimal estimatedEffort)
    {
        var workPackage = GetWorkPackage(workPackageId);
        EnsureIsCurrentTeamMember(responsiblePersonId);

        var task = workPackage.AddTask(name, description, responsiblePersonId, estimatedEffort);

        RaiseDomainEvent(new TaskAdded(
            Id, workPackage.Id, task.Id, task.Name, task.Description, task.ResponsiblePersonId, task.EstimatedEffort));
        return task.Id;
    }

    public void CompleteWorkPackage(Guid workPackageId)
    {
        var workPackage = GetWorkPackage(workPackageId);

        workPackage.Complete();
        RaiseDomainEvent(new WorkPackageCompleted(Id, workPackage.Id));
    }

    public void CancelWorkPackage(Guid workPackageId, string reason)
    {
        var workPackage = GetWorkPackage(workPackageId);

        var cancelledTaskIds = workPackage.Cancel(reason);
        RaiseDomainEvent(new WorkPackageCancelled(Id, workPackage.Id, workPackage.CancellationReason!, cancelledTaskIds));

        RemoveDependenciesOf(cancelledTaskIds);
    }

    public void StartTask(Guid workPackageId, Guid taskId)
    {
        var workPackage = GetWorkPackage(workPackageId);

        workPackage.StartTask(taskId);
        RaiseDomainEvent(new TaskStarted(Id, workPackage.Id, taskId));
    }

    public void CompleteTask(Guid workPackageId, Guid taskId)
    {
        var workPackage = GetWorkPackage(workPackageId);

        workPackage.CompleteTask(taskId);
        RaiseDomainEvent(new TaskCompleted(Id, workPackage.Id, taskId));
    }

    public void CancelTask(Guid workPackageId, Guid taskId)
    {
        var workPackage = GetWorkPackage(workPackageId);

        workPackage.CancelTask(taskId);
        RaiseDomainEvent(new TaskCancelled(Id, workPackage.Id, taskId));

        RemoveDependenciesOf([taskId]);
    }

    /// <summary>
    /// Only a member of the project team on <paramref name="date"/> can log work,
    /// so an hourly rate for that date always exists.
    /// </summary>
    public void LogWork(Guid workPackageId, Guid taskId, Guid employeeId, DateOnly date, decimal hours)
    {
        var workPackage = GetWorkPackage(workPackageId);

        if (!IsTeamMemberOn(employeeId, date))
            throw new DomainException($"Employee '{employeeId}' is not a member of the project team on {date}.");

        var timeEntry = workPackage.LogWork(taskId, employeeId, date, hours);
        RaiseDomainEvent(new WorkLogged(Id, workPackage.Id, taskId, employeeId, timeEntry.Date, timeEntry.Hours));
    }

    private WorkPackage GetWorkPackage(Guid workPackageId) =>
        _workPackages.SingleOrDefault(wp => wp.Id == workPackageId)
        ?? throw new EntityNotFoundException(nameof(WorkPackage), workPackageId);

    private void EnsureIsCurrentTeamMember(Guid employeeId)
    {
        if (!IsTeamMemberOn(employeeId, DateOnly.FromDateTime(DateTime.UtcNow)))
            throw new DomainException($"Employee '{employeeId}' is not a member of the project team.");
    }
}
