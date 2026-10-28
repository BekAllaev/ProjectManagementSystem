using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetWorkPackages;

[DataContract]
public sealed record WorkPackageList(
    [property: DataMember(Order = 1)] IReadOnlyList<WorkPackageListItem> Items)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private WorkPackageList() : this(new List<WorkPackageListItem>())
    {
    }
}

/// <param name="IsRegistered">
/// Registered in the project. A new package is registered shortly after its creation (eventual consistency).
/// </param>
/// <param name="CountedTasks">Tasks that are not cancelled.</param>
/// <param name="Version">Version of the aggregate it is read from (see the read side in docs/domain-model.md).</param>
[DataContract]
public sealed record WorkPackageListItem(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string Description,
    [property: DataMember(Order = 3)] EmployeeRef ResponsiblePerson,
    [property: DataMember(Order = 4)] WorkPackageStatus Status,
    [property: DataMember(Order = 5)] string? CancellationReason,
    [property: DataMember(Order = 6)] bool IsRegistered,
    [property: DataMember(Order = 7)] int CompletedTasks,
    [property: DataMember(Order = 8)] int CountedTasks,
    [property: DataMember(Order = 9)] IReadOnlyList<WorkPackageTaskItem> Tasks,
    [property: DataMember(Order = 10)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private WorkPackageListItem() : this(
        default, default!, default!, default, default, default, default, default, new List<WorkPackageTaskItem>(),
        default)
    {
    }
}

[DataContract]
public sealed record WorkPackageTaskItem(
    [property: DataMember(Order = 1)] Guid TaskId,
    [property: DataMember(Order = 2)] string Name,
    [property: DataMember(Order = 3)] EmployeeRef ResponsiblePerson,
    [property: DataMember(Order = 4)] ProjectTaskStatus Status)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private WorkPackageTaskItem() : this(default, default!, default!, default)
    {
    }
}
