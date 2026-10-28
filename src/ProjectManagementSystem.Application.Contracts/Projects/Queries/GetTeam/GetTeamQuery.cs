using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Queries.GetTeam;

/// <summary>
/// Team of a project, one item per employee with all their membership periods; current members first, then by name.
/// </summary>
[DataContract]
public sealed record GetTeamQuery(
    [property: DataMember(Order = 1)] Guid ProjectId) : IQuery<Team>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetTeamQuery() : this(Guid.Empty)
    {
    }
}
