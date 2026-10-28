using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Milestones;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Milestones.Queries.GetMilestones;

[DataContract]
public sealed record MilestoneList(
    [property: DataMember(Order = 1)] IReadOnlyList<MilestoneItem> Items)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private MilestoneList() : this(new List<MilestoneItem>())
    {
    }
}

/// <param name="DaysLate">Days since the target date while the milestone is open; 0 otherwise.</param>
/// <param name="CountedTasks">
/// Tasks that are not cancelled; cancelled tasks stay in the milestone but are not counted.
/// </param>
/// <param name="Version">Version of the aggregate it is read from (see the read side in docs/domain-model.md).</param>
[DataContract]
public sealed record MilestoneItem(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string Name,
    [property: DataMember(Order = 3)] string Description,
    [property: DataMember(Order = 4)] DateOnly TargetDate,
    [property: DataMember(Order = 5)] MilestoneStatus Status,
    [property: DataMember(Order = 6)] DateTime? CompletedAt,
    [property: DataMember(Order = 7)] MilestoneHealth Health,
    [property: DataMember(Order = 8)] int DaysLate,
    [property: DataMember(Order = 9)] int CompletedTasks,
    [property: DataMember(Order = 10)] int CountedTasks,
    [property: DataMember(Order = 11)] int ProgressPercent,
    [property: DataMember(Order = 12)] IReadOnlyList<MilestoneTaskItem> Tasks,
    [property: DataMember(Order = 13)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private MilestoneItem() : this(
        default, default!, default!, default, default, default, default, default, default, default, default,
        new List<MilestoneTaskItem>(), default)
    {
    }
}

[DataContract]
public sealed record MilestoneTaskItem(
    [property: DataMember(Order = 1)] Guid TaskId,
    [property: DataMember(Order = 2)] Guid WorkPackageId,
    [property: DataMember(Order = 3)] string Name,
    [property: DataMember(Order = 4)] string WorkPackageDescription,
    [property: DataMember(Order = 5)] ProjectTaskStatus Status,
    [property: DataMember(Order = 6)] bool IsCounted)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private MilestoneTaskItem() : this(default, default, default!, default!, default, default)
    {
    }
}
