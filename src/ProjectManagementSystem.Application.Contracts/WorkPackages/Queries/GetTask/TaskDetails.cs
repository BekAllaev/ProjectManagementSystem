using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetTask;

/// <param name="CanChangeStatus">
/// The task is not finished and the caller is its responsible person or that of its work package.
/// </param>
/// <param name="CanLogTime">
/// The task is in progress and the caller is a member of the team today; the caller logs only their own time.
/// </param>
/// <param name="TimeEntries">Newest work date first.</param>
/// <param name="Version">Version of the work package, which holds the task.</param>
/// <param name="TimeLogVersion">Version of the time log of the task; 0 before the first entry.</param>
[DataContract]
public sealed record TaskDetails(
    [property: DataMember(Order = 1)] Guid TaskId,
    [property: DataMember(Order = 2)] string Name,
    [property: DataMember(Order = 3)] string Description,
    [property: DataMember(Order = 4)] Guid ProjectId,
    [property: DataMember(Order = 5)] Guid WorkPackageId,
    [property: DataMember(Order = 6)] string WorkPackageDescription,
    [property: DataMember(Order = 7)] EmployeeRef ResponsiblePerson,
    [property: DataMember(Order = 8)] ProjectTaskStatus Status,
    [property: DataMember(Order = 9)] decimal EstimatedEffort,
    [property: DataMember(Order = 10)] bool HasUnfinishedFinishToStartPredecessor,
    [property: DataMember(Order = 11)] bool CanChangeStatus,
    [property: DataMember(Order = 12)] bool CanLogTime,
    [property: DataMember(Order = 13)] IReadOnlyList<TaskDependencyItem> Predecessors,
    [property: DataMember(Order = 14)] IReadOnlyList<TaskDependencyItem> Successors,
    [property: DataMember(Order = 15)] IReadOnlyList<TimeEntryItem> TimeEntries,
    [property: DataMember(Order = 16)] decimal LoggedHours,
    [property: DataMember(Order = 17)] decimal LoggedCost,
    [property: DataMember(Order = 18)] string Currency,
    [property: DataMember(Order = 19)] long Version,
    [property: DataMember(Order = 20)] long TimeLogVersion)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private TaskDetails() : this(
        default, default!, default!, default, default, default!, default!, default, default, default, default, default,
        new List<TaskDependencyItem>(), new List<TaskDependencyItem>(), new List<TimeEntryItem>(), default, default,
        default!, default, default)
    {
    }
}

[DataContract]
public sealed record TaskDependencyItem(
    [property: DataMember(Order = 1)] Guid TaskId,
    [property: DataMember(Order = 2)] Guid WorkPackageId,
    [property: DataMember(Order = 3)] string Name,
    [property: DataMember(Order = 4)] string WorkPackageDescription,
    [property: DataMember(Order = 5)] bool IsInOtherWorkPackage,
    [property: DataMember(Order = 6)] ProjectTaskStatus Status,
    [property: DataMember(Order = 7)] DependencyType Type)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private TaskDependencyItem() : this(default, default, default!, default!, default, default, default)
    {
    }
}

/// <param name="HourlyRate">Rate valid on the work date, kept as it was when the time was logged.</param>
/// <param name="CanRemove">The caller logged the entry and the task is still in progress.</param>
[DataContract]
public sealed record TimeEntryItem(
    [property: DataMember(Order = 1)] Guid EntryId,
    [property: DataMember(Order = 2)] EmployeeRef Employee,
    [property: DataMember(Order = 3)] DateOnly WorkDate,
    [property: DataMember(Order = 4)] decimal Hours,
    [property: DataMember(Order = 5)] decimal HourlyRate,
    [property: DataMember(Order = 6)] decimal Cost,
    [property: DataMember(Order = 7)] bool CanRemove)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private TimeEntryItem() : this(default, default!, default, default, default, default, default)
    {
    }
}
