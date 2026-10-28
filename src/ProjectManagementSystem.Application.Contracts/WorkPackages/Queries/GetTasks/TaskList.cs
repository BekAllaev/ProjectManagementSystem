using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetTasks;

/// <param name="Counts">Counts of all tasks of the project, without the filters, for the status filter.</param>
[DataContract]
public sealed record TaskList(
    [property: DataMember(Order = 1)] IReadOnlyList<TaskListItem> Items,
    [property: DataMember(Order = 2)] TaskStatusCounts Counts)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private TaskList() : this(new List<TaskListItem>(), default!)
    {
    }
}

/// <param name="HasUnfinishedFinishToStartPredecessor">
/// The task is not finished and a finish-to-start predecessor is not completed: a warning, starting is allowed.
/// </param>
/// <param name="CanChangeStatus">
/// The task is not finished and the caller is its responsible person or that of its work package.
/// </param>
/// <param name="Version">Version of the work package, which holds the task.</param>
[DataContract]
public sealed record TaskListItem(
    [property: DataMember(Order = 1)] Guid TaskId,
    [property: DataMember(Order = 2)] string Name,
    [property: DataMember(Order = 3)] Guid WorkPackageId,
    [property: DataMember(Order = 4)] string WorkPackageDescription,
    [property: DataMember(Order = 5)] EmployeeRef ResponsiblePerson,
    [property: DataMember(Order = 6)] ProjectTaskStatus Status,
    [property: DataMember(Order = 7)] decimal EstimatedEffort,
    [property: DataMember(Order = 8)] decimal LoggedHours,
    [property: DataMember(Order = 9)] bool HasUnfinishedFinishToStartPredecessor,
    [property: DataMember(Order = 10)] bool CanChangeStatus,
    [property: DataMember(Order = 11)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private TaskListItem() : this(
        default, default!, default, default!, default!, default, default, default, default, default, default)
    {
    }
}
