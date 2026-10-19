using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Domain.Projects.WorkPackages;

/// <summary>
/// Group of related tasks. It is changed only through <see cref="Project"/>, which checks
/// the rules that need the team of the project.
/// </summary>
public sealed class WorkPackage : Entity<Guid>
{
    private readonly List<ProjectTask> _tasks = [];

    public Guid ProjectId { get; }

    public string Description { get; private set; }

    public Guid ResponsiblePersonId { get; private set; }

    public WorkPackageStatus Status { get; private set; }

    public string? CancellationReason { get; private set; }

    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();

    internal WorkPackage(Guid id, Guid projectId, string description, Guid responsiblePersonId) : base(id)
    {
        ProjectId = projectId;
        Description = Guard.NotEmpty(description, nameof(Description));
        ResponsiblePersonId = responsiblePersonId;
        Status = WorkPackageStatus.Open;
    }

    public bool ContainsTask(Guid taskId) => _tasks.Any(t => t.Id == taskId);

    public ProjectTask GetTask(Guid taskId) =>
        _tasks.SingleOrDefault(t => t.Id == taskId) ?? throw new EntityNotFoundException("Task", taskId);

    internal void ChangeResponsiblePerson(Guid employeeId) => ResponsiblePersonId = employeeId;

    internal ProjectTask AddTask(string name, string description, Guid responsiblePersonId, decimal estimatedEffort)
    {
        EnsureOpen("add a task to");

        var task = new ProjectTask(Guid.NewGuid(), Id, name, description, responsiblePersonId, estimatedEffort);
        _tasks.Add(task);
        return task;
    }

    internal void Complete()
    {
        EnsureOpen("complete");

        if (_tasks.Any(t => !t.IsFinished))
            throw new DomainException("Work package can be completed only when all its tasks are completed or cancelled.");

        Status = WorkPackageStatus.Completed;
    }

    /// <summary>
    /// Cancels the package and all its unfinished tasks. Completed tasks stay completed.
    /// </summary>
    /// <returns>Ids of the tasks that were cancelled by this call.</returns>
    internal IReadOnlyList<Guid> Cancel(string reason)
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
        return cancelledTaskIds;
    }

    internal void StartTask(Guid taskId) => GetTask(taskId).Start();

    internal void CompleteTask(Guid taskId) => GetTask(taskId).Complete();

    internal void CancelTask(Guid taskId) => GetTask(taskId).Cancel();

    private void EnsureOpen(string action)
    {
        if (Status != WorkPackageStatus.Open)
            throw new DomainException($"Cannot {action} a work package that is {Status}.");
    }
}
