using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Queries.GetRoles;

/// <summary>
/// Role catalog of a project, ordered by name.
/// </summary>
[DataContract]
public sealed record GetRolesQuery(
    [property: DataMember(Order = 1)] Guid ProjectId) : IQuery<RoleList>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetRolesQuery() : this(Guid.Empty)
    {
    }
}
