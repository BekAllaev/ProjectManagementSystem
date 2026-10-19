using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Domain.Projects.WorkPackages;

/// <summary>
/// Task of a work package. Named ProjectTask to avoid a clash with <see cref="System.Threading.Tasks.Task"/>.
/// It is changed only through its <see cref="WorkPackage"/>.
/// </summary>
public sealed class ProjectTask : Entity<Guid>
{
    private readonly List<TimeEntry> _timeEntries = [];

    public Guid WorkPackageId { get; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public Guid ResponsiblePersonId { get; private set; }

    public ProjectTaskStatus Status { get; private set; }

    /// <summary>
    /// Estimated effort in hours.
    /// </summary>
    public decimal EstimatedEffort { get; private set; }

    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();

    public bool IsFinished => Status is ProjectTaskStatus.Completed or ProjectTaskStatus.Cancelled;

    internal ProjectTask(
        Guid id,
        Guid workPackageId,
        string name,
        string description,
        Guid responsiblePersonId,
        decimal estimatedEffort) : base(id)
    {
        WorkPackageId = workPackageId;
        Name = Guard.NotEmpty(name, nameof(Name));
        Description = description ?? string.Empty;
        ResponsiblePersonId = responsiblePersonId;
        EstimatedEffort = Guard.Positive(estimatedEffort, nameof(EstimatedEffort));
        Status = ProjectTaskStatus.Open;
    }

    internal void Start()
    {
        EnsureStatus(ProjectTaskStatus.Open, "started");
        Status = ProjectTaskStatus.InProgress;
    }

    internal void Complete()
    {
        EnsureStatus(ProjectTaskStatus.InProgress, "completed");
        Status = ProjectTaskStatus.Completed;
    }

    internal void Cancel()
    {
        if (IsFinished)
            throw new DomainException($"Task '{Name}' cannot be cancelled because it is already {Status}.");

        Status = ProjectTaskStatus.Cancelled;
    }

    internal TimeEntry LogWork(Guid employeeId, DateOnly date, decimal hours)
    {
        EnsureStatus(ProjectTaskStatus.InProgress, "logged work on");

        var timeEntry = new TimeEntry(employeeId, date, hours);
        _timeEntries.Add(timeEntry);
        return timeEntry;
    }

    private void EnsureStatus(ProjectTaskStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException($"Task '{Name}' cannot be {action} because it is {Status}, expected {expected}.");
    }
}
