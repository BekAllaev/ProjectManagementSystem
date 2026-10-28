using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetWorkPackages;

/// <summary>
/// Work packages of a project with their tasks, in the order they were created.
/// </summary>
[DataContract]
public sealed record GetWorkPackagesQuery(
    [property: DataMember(Order = 1)] Guid ProjectId) : IQuery<WorkPackageList>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetWorkPackagesQuery() : this(Guid.Empty)
    {
    }
}
