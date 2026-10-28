using System.Runtime.Serialization;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;

/// <param name="OpenTasks">
/// Open and in-progress tasks the caller is responsible for, in all projects; in progress first.
/// </param>
/// <param name="Projects">Projects whose team the caller is a member of today, ordered by title.</param>
/// <param name="FundingAlerts">
/// Those of <paramref name="Projects"/> that used 80% of their funding or more, or are overspent; overspent first.
/// </param>
[DataContract]
public sealed record MyDashboard(
    [property: DataMember(Order = 1)] IReadOnlyList<MyTaskItem> OpenTasks,
    [property: DataMember(Order = 2)] IReadOnlyList<MyProjectItem> Projects,
    [property: DataMember(Order = 3)] IReadOnlyList<FundingAlertItem> FundingAlerts)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private MyDashboard() : this(new List<MyTaskItem>(), new List<MyProjectItem>(), new List<FundingAlertItem>())
    {
    }
}

/// <param name="Version">Version of the work package, which holds the task.</param>
[DataContract]
public sealed record MyTaskItem(
    [property: DataMember(Order = 1)] Guid TaskId,
    [property: DataMember(Order = 2)] string Name,
    [property: DataMember(Order = 3)] Guid ProjectId,
    [property: DataMember(Order = 4)] string ProjectTitle,
    [property: DataMember(Order = 5)] Guid WorkPackageId,
    [property: DataMember(Order = 6)] string WorkPackageDescription,
    [property: DataMember(Order = 7)] ProjectTaskStatus Status,
    [property: DataMember(Order = 8)] decimal EstimatedEffort,
    [property: DataMember(Order = 9)] decimal LoggedHours,
    [property: DataMember(Order = 10)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private MyTaskItem() : this(
        default, default!, default, default!, default, default!, default, default, default, default)
    {
    }
}

/// <param name="Version">Version of the aggregate it is read from (see the read side in docs/domain-model.md).</param>
[DataContract]
public sealed record MyProjectItem(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string Title,
    [property: DataMember(Order = 3)] ProjectState State,
    [property: DataMember(Order = 4)] int ProgressPercent,
    [property: DataMember(Order = 5)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private MyProjectItem() : this(default, default!, default, default, default)
    {
    }
}

/// <param name="UsedPercent">Actual cost of total funding, rounded to one decimal; empty without funding.</param>
[DataContract]
public sealed record FundingAlertItem(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] string ProjectTitle,
    [property: DataMember(Order = 3)] string Currency,
    [property: DataMember(Order = 4)] decimal TotalFunding,
    [property: DataMember(Order = 5)] decimal ActualCost,
    [property: DataMember(Order = 6)] decimal RemainingFunding,
    [property: DataMember(Order = 7)] decimal? UsedPercent,
    [property: DataMember(Order = 8)] bool IsOverspent)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private FundingAlertItem() : this(default, default!, default!, default, default, default, default, default)
    {
    }
}
