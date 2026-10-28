using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// Row of the list of dependencies (<c>DependencyItem</c>) and of the dependencies of a task
/// (<c>TaskDependencyItem</c>).
/// </summary>
public sealed class DependencyItemView
{
    public Guid ProjectId { get; set; }

    public Guid PredecessorTaskId { get; set; }

    public Guid SuccessorTaskId { get; set; }

    public Guid PredecessorWorkPackageId { get; set; }

    public string PredecessorName { get; set; } = string.Empty;

    public string PredecessorWorkPackageDescription { get; set; } = string.Empty;

    public ProjectTaskStatus PredecessorStatus { get; set; }

    public Guid SuccessorWorkPackageId { get; set; }

    public string SuccessorName { get; set; } = string.Empty;

    public string SuccessorWorkPackageDescription { get; set; } = string.Empty;

    public ProjectTaskStatus SuccessorStatus { get; set; }

    public DependencyType Type { get; set; }

    public bool IsAcrossWorkPackages { get; set; }

    /// <summary>
    /// Finish-to-start, the predecessor is not completed and the successor is not finished.
    /// </summary>
    public bool HasWarning { get; set; }
}
