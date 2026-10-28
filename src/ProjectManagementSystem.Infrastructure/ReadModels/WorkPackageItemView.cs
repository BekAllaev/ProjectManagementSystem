using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// Row of the list of work packages (<c>WorkPackageListItem</c>); its tasks are the <see cref="TaskItemView"/> of the
/// package.
/// </summary>
public sealed class WorkPackageItemView
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Description { get; set; } = string.Empty;

    public Guid ResponsiblePersonId { get; set; }

    public WorkPackageStatus Status { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRegistered { get; set; }

    /// <summary>
    /// Packages are listed in the order they were registered; an unregistered one comes last.
    /// </summary>
    public DateTime? RegisteredAt { get; set; }

    public int CompletedTasks { get; set; }

    /// <summary>
    /// Tasks that are not cancelled.
    /// </summary>
    public int CountedTasks { get; set; }

    /// <summary>
    /// Version of the work package (see <c>IDomainEvent.AggregateVersion</c>).
    /// </summary>
    public long Version { get; set; }
}
