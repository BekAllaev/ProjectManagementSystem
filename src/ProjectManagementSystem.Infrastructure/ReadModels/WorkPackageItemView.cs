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
    /// 1 for the first package registered in the project, then one more for every registration; empty until the
    /// package is registered. Packages are listed in this order, an unregistered one last.
    /// </summary>
    public int? RegistrationPosition { get; set; }

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
