using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Projects.Funding;
using ProjectManagementSystem.Domain.Projects.Schedule;

namespace ProjectManagementSystem.Domain.Projects;

/// <summary>
/// Root of the project aggregate. For now it is a "mega aggregate" that owns everything
/// related to a project; each area lives in its own partial file so it can be cut out
/// into a separate aggregate later.
/// </summary>
public sealed partial class Project : AggregateRoot<Guid>
{
    public string Title { get; private set; }

    public string Description { get; private set; }

    public ProjectState State { get; private set; }

    public DateTime CreationDate { get; }

    public DateTime? StartDate { get; private set; }

    public DateTime? CompletionDate { get; private set; }

    public Project(Guid id, string title, string description, Currency currency) : base(id)
    {
        Title = Guard.NotEmpty(title, nameof(Title));
        Description = description ?? string.Empty;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        State = ProjectState.Created;
        CreationDate = DateTime.UtcNow;
        ScheduleNetwork = new ScheduleNetwork(Guid.NewGuid(), id);

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

    public void Complete()
    {
        if (State != ProjectState.InProgress)
            throw new DomainException($"Only a project in state {ProjectState.InProgress} can be completed. Current state: {State}.");

        SetCompletionDate(DateTime.UtcNow);
        State = ProjectState.Completed;

        RaiseDomainEvent(new ProjectCompleted(Id, CompletionDate!.Value));
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
