using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects.Events;

namespace ProjectManagementSystem.Domain.Projects;

/// <summary>
/// Root of the project aggregate: life cycle of the project, its roles, its team with hourly rates and
/// the ids of its work packages. Work packages, the schedule network, time logs, milestones, funding and risks are separate
/// aggregates that refer to the project by id.
/// </summary>
public sealed partial class Project : AggregateRoot<Guid>
{
    public string Title { get; private set; }

    public string Description { get; private set; }

    public ProjectState State { get; private set; }

    public DateTime CreationDate { get; }

    public DateTime? StartDate { get; private set; }

    public DateTime? CompletionDate { get; private set; }

    /// <summary>
    /// Currency of all money of the project: hourly rates, time entries and funding.
    /// </summary>
    public Currency Currency { get; }

    /// <summary>
    /// Created and InProgress projects are open: planning happens before the start.
    /// </summary>
    public bool IsOpen => State != ProjectState.Completed;

    public Project(Guid id, string title, string description, Currency currency) : base(id)
    {
        Title = Guard.NotEmpty(title, nameof(Title));
        Description = description ?? string.Empty;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        State = ProjectState.Created;
        CreationDate = DateTime.UtcNow;

        RaiseDomainEvent(new ProjectCreated(Id, Title, Currency.Code, CreationDate));
    }

    public void Start()
    {
        if (State != ProjectState.Created)
            throw new DomainException($"Only a project in state {ProjectState.Created} can be started. Current state: {State}.");

        SetStartDate(DateTime.UtcNow);
        State = ProjectState.InProgress;

        RaiseDomainEvent(new ProjectStarted(Id, StartDate!.Value));
    }

    /// <summary>
    /// Goes through <see cref="Services.ProjectClosureService"/>, which checks that every work package
    /// of the project is finished.
    /// </summary>
    internal void Complete(DateTime completionDate)
    {
        EnsureOpen("complete");

        if (State != ProjectState.InProgress)
            throw new DomainException($"Only a project in state {ProjectState.InProgress} can be completed. Current state: {State}.");

        SetCompletionDate(completionDate);
        State = ProjectState.Completed;

        RaiseDomainEvent(new ProjectCompleted(Id, CompletionDate!.Value));
    }

    private void EnsureOpen(string action)
    {
        if (!IsOpen)
            throw new DomainException($"Cannot {action} a project that is {State}.");
    }

    private void SetStartDate(DateTime startDate)
    {
        if (StartDate is not null)
            throw new DomainException("Start date of the project has already been set.");

        StartDate = startDate;
    }

    private void SetCompletionDate(DateTime completionDate)
    {
        if (CompletionDate is not null)
            throw new DomainException("Completion date of the project has already been set.");

        if (StartDate is null || completionDate < StartDate)
            throw new DomainException("Completion date must not be earlier than the start date.");

        CompletionDate = completionDate;
    }
}
