using System.Runtime.Serialization;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Schedule.Queries.GetDependencies;

/// <param name="Version">Version of the schedule network of the project; 0 before the first dependency.</param>
[DataContract]
public sealed record DependencyList(
    [property: DataMember(Order = 1)] IReadOnlyList<DependencyItem> Items,
    [property: DataMember(Order = 2)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private DependencyList() : this(new List<DependencyItem>(), default)
    {
    }
}

/// <param name="HasWarning">
/// Finish-to-start, the predecessor is not completed and the successor is not finished.
/// </param>
[DataContract]
public sealed record DependencyItem(
    [property: DataMember(Order = 1)] DependencyTaskItem Predecessor,
    [property: DataMember(Order = 2)] DependencyTaskItem Successor,
    [property: DataMember(Order = 3)] DependencyType Type,
    [property: DataMember(Order = 4)] bool IsAcrossWorkPackages,
    [property: DataMember(Order = 5)] bool HasWarning)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private DependencyItem() : this(default!, default!, default, default, default)
    {
    }
}

[DataContract]
public sealed record DependencyTaskItem(
    [property: DataMember(Order = 1)] Guid TaskId,
    [property: DataMember(Order = 2)] Guid WorkPackageId,
    [property: DataMember(Order = 3)] string Name,
    [property: DataMember(Order = 4)] string WorkPackageDescription,
    [property: DataMember(Order = 5)] ProjectTaskStatus Status)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private DependencyTaskItem() : this(default, default, default!, default!, default)
    {
    }
}
