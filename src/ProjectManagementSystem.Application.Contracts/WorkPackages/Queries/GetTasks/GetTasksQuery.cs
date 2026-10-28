using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetTasks;

/// <summary>
/// Tasks of all work packages of a project. Every filter is optional.
/// </summary>
[DataContract]
public sealed record GetTasksQuery(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] ProjectTaskStatus? Status,
    [property: DataMember(Order = 3)] Guid? WorkPackageId,
    [property: DataMember(Order = 4)] Guid? ResponsiblePersonId) : IQuery<TaskList>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetTasksQuery() : this(default, default, default, default)
    {
    }
}
