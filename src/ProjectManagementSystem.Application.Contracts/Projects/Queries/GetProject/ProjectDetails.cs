using System.Runtime.Serialization;
using ProjectManagementSystem.Application.WorkPackages;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Queries.GetProject;

/// <param name="IsOpen">Work packages, milestones, dependencies and funding can still be added.</param>
/// <param name="TaskCounts">Tasks of all work packages of the project, registered or not.</param>
/// <param name="CountedTasks">Tasks that are not cancelled.</param>
/// <param name="ProgressPercent">Completed of the counted tasks, rounded; 0 without counted tasks.</param>
/// <param name="Version">Version of the aggregate it is read from (see the read side in docs/domain-model.md).</param>
[DataContract]
public sealed record ProjectDetails(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string Title,
    [property: DataMember(Order = 3)] string Description,
    [property: DataMember(Order = 4)] ProjectState State,
    [property: DataMember(Order = 5)] bool IsOpen,
    [property: DataMember(Order = 6)] string Currency,
    [property: DataMember(Order = 7)] DateTime CreationDate,
    [property: DataMember(Order = 8)] DateTime? StartDate,
    [property: DataMember(Order = 9)] DateTime? CompletionDate,
    [property: DataMember(Order = 10)] TaskStatusCounts TaskCounts,
    [property: DataMember(Order = 11)] int CountedTasks,
    [property: DataMember(Order = 12)] int ProgressPercent,
    [property: DataMember(Order = 13)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ProjectDetails() : this(
        default, default!, default!, default, default, default!, default, default, default, default!, default, default,
        default)
    {
    }
}
