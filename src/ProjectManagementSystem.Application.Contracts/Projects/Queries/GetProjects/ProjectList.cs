using System.Runtime.Serialization;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Queries.GetProjects;

[DataContract]
public sealed record ProjectList(
    [property: DataMember(Order = 1)] IReadOnlyList<ProjectListItem> Items)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ProjectList() : this(new List<ProjectListItem>())
    {
    }
}

/// <param name="CountedTasks">Tasks that are not cancelled.</param>
/// <param name="ProgressPercent">Completed of the counted tasks, rounded; 0 without counted tasks.</param>
/// <param name="RemainingFunding">Total funding minus actual cost; negative when overspent.</param>
/// <param name="Version">Version of the aggregate it is read from (see the read side in docs/domain-model.md).</param>
[DataContract]
public sealed record ProjectListItem(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string Title,
    [property: DataMember(Order = 3)] string Description,
    [property: DataMember(Order = 4)] ProjectState State,
    [property: DataMember(Order = 5)] string Currency,
    [property: DataMember(Order = 6)] DateTime CreationDate,
    [property: DataMember(Order = 7)] DateTime? StartDate,
    [property: DataMember(Order = 8)] DateTime? CompletionDate,
    [property: DataMember(Order = 9)] int CompletedTasks,
    [property: DataMember(Order = 10)] int CountedTasks,
    [property: DataMember(Order = 11)] int ProgressPercent,
    [property: DataMember(Order = 12)] decimal TotalFunding,
    [property: DataMember(Order = 13)] decimal ActualCost,
    [property: DataMember(Order = 14)] decimal RemainingFunding,
    [property: DataMember(Order = 15)] bool IsOverspent,
    [property: DataMember(Order = 16)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ProjectListItem() : this(
        default, default!, default!, default, default!, default, default, default, default, default, default, default,
        default, default, default, default)
    {
    }
}
