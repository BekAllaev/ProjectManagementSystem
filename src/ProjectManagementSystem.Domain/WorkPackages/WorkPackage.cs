using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Domain.WorkPackages;

/// <summary>
/// Group of related tasks of a project. Operations that assign a responsible person are internal:
/// they go through <see cref="Services.WorkPackageService"/>, which checks that the person is
/// a member of the project team.
/// </summary>
public sealed class WorkPackage : AggregateRoot<Guid>
{
    private readonly List<ProjectTask> _tasks = [];

    public Guid ProjectId { get; }

    public string Description { get; private set; }

    public Guid ResponsiblePersonId { get; private set; }

    public WorkPackageStatus Status { get; private set; }

    public string? CancellationReason { get; private set; }

    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();

    /// <summary>
    /// For EF Core materialization only; does not raise domain events.
    /// </summary>
    private WorkPackage()
    {
        Description = null!;
    }

    internal WorkPackage(Guid id, Guid projectId, string description, Guid responsiblePersonId) : base(id)
    {
        ProjectId = projectId;
        Description = Guard.NotEmpty(description, nameof(Description));
        ResponsiblePersonId = responsiblePersonId;
        Status = WorkPackageStatus.Open;

        RaiseDomainEvent(new WorkPackageCreated(ProjectId, Id, Description, ResponsiblePersonId));
    }

    public bool ContainsTask(Guid taskId) => _tasks.Any(t => t.Id == taskId);

    public ProjectTask GetTask(Guid taskId) =>
        _tasks.SingleOrDefault(t => t.Id == taskId) ?? throw new EntityNotFoundException("Task", taskId);

    internal void ChangeResponsiblePerson(Guid employeeId)
    {
        ResponsiblePersonId = employeeId;
        RaiseDomainEvent(new ResponsiblePersonChanged(ProjectId, Id, TaskId: null, employeeId));
    }

    internal ProjectTask AddTask(string name, string description, Guid responsiblePersonId, decimal estimatedEffort)
    {
        EnsureOpen("add a task to");

        var task = new ProjectTask(Guid.NewGuid(), Id, name, description, responsiblePersonId, estimatedEffort);
        _tasks.Add(task);

        RaiseDomainEvent(new TaskAdded(
            ProjectId, Id, task.Id, task.Name, task.Description, task.ResponsiblePersonId, task.EstimatedEffort));
        return task;
    }

    internal void ChangeTaskResponsiblePerson(Guid taskId, Guid employeeId)
    {
        GetTask(taskId).ChangeResponsiblePerson(employeeId);
        RaiseDomainEvent(new ResponsiblePersonChanged(ProjectId, Id, taskId, employeeId));
    }

    public void StartTask(Guid taskId)
    {
        GetTask(taskId).Start();
        RaiseDomainEvent(new TaskStarted(ProjectId, Id, taskId));
    }

    public void CompleteTask(Guid taskId)
    {
        GetTask(taskId).Complete();
        RaiseDomainEvent(new TaskCompleted(ProjectId, Id, taskId));
    }

    public void CancelTask(Guid taskId)
    {
        GetTask(taskId).Cancel();
        RaiseDomainEvent(new TaskCancelled(ProjectId, Id, taskId));
    }

    public void Complete()
    {
        EnsureOpen("complete");

        if (_tasks.Any(t => !t.IsFinished))
            throw new DomainRuleViolationException("Work package can be completed only when all its tasks are completed or cancelled.");

        Status = WorkPackageStatus.Completed;
        RaiseDomainEvent(new WorkPackageCompleted(ProjectId, Id));
    }

    /// <summary>
    /// Cancels the package and all its unfinished tasks. Completed tasks stay completed.
    /// </summary>
    public void Cancel(string reason)
    {
        EnsureOpen("cancel");
        reason = Guard.NotEmpty(reason, nameof(CancellationReason));

        var cancelledTaskIds = new List<Guid>();
        foreach (var task in _tasks.Where(t => !t.IsFinished))
        {
            task.Cancel();
            cancelledTaskIds.Add(task.Id);
        }

        Status = WorkPackageStatus.Cancelled;
        CancellationReason = reason;
        RaiseDomainEvent(new WorkPackageCancelled(ProjectId, Id, reason, cancelledTaskIds));
    }

    private void EnsureOpen(string action)
    {
        if (Status != WorkPackageStatus.Open)
            throw new DomainRuleViolationException($"Cannot {action} a work package that is {Status}.");
    }
}
