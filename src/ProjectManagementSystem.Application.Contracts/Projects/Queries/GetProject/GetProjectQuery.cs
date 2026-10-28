using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Queries.GetProject;

/// <summary>
/// Header of the page of a project.
/// </summary>
[DataContract]
public sealed record GetProjectQuery(
    [property: DataMember(Order = 1)] Guid ProjectId) : IQuery<ProjectDetails>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetProjectQuery() : this(Guid.Empty)
    {
    }
}
