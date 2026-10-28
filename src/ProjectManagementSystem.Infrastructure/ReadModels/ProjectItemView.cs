using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// Row of the list of projects and header of a project (<c>ProjectListItem</c>, <c>ProjectDetails</c>), with the
/// funding figures of <c>FundingSummary</c> and the dashboard.
/// </summary>
public sealed class ProjectItemView
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ProjectState State { get; set; }

    public bool IsOpen { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime CreationDate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? CompletionDate { get; set; }

    public int OpenTasks { get; set; }

    public int InProgressTasks { get; set; }

    public int CompletedTasks { get; set; }

    public int CancelledTasks { get; set; }

    /// <summary>
    /// Tasks that are not cancelled.
    /// </summary>
    public int CountedTasks { get; set; }

    public int ProgressPercent { get; set; }

    public decimal TotalFunding { get; set; }

    public decimal ActualCost { get; set; }

    public decimal RemainingFunding { get; set; }

    /// <summary>
    /// Empty without funding.
    /// </summary>
    public decimal? UsedPercent { get; set; }

    public bool IsOverspent { get; set; }

    /// <summary>
    /// Version of the project (see <c>IDomainEvent.AggregateVersion</c>).
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Version of the schedule network of the project; 0 before it exists.
    /// </summary>
    public long ScheduleVersion { get; set; }

    /// <summary>
    /// Version of the funding of the project; 0 before it exists.
    /// </summary>
    public long FundingVersion { get; set; }
}
