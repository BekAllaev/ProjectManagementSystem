using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetTask;

/// <summary>
/// One task with its dependencies and time log.
/// </summary>
[DataContract]
public sealed record GetTaskQuery(
    [property: DataMember(Order = 1)] Guid WorkPackageId,
    [property: DataMember(Order = 2)] Guid TaskId) : IQuery<TaskDetails>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetTaskQuery() : this(default, default)
    {
    }
}
