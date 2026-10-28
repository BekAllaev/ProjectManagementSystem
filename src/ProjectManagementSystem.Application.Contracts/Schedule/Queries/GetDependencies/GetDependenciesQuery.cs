using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Schedule.Queries.GetDependencies;

/// <summary>
/// Dependencies between the tasks of a project.
/// </summary>
[DataContract]
public sealed record GetDependenciesQuery(
    [property: DataMember(Order = 1)] Guid ProjectId) : IQuery<DependencyList>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetDependenciesQuery() : this(Guid.Empty)
    {
    }
}
