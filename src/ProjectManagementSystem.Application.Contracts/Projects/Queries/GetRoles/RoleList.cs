using System.Runtime.Serialization;

namespace ProjectManagementSystem.Application.Projects.Queries.GetRoles;

/// <param name="Version">Version of the project, which holds the roles.</param>
[DataContract]
public sealed record RoleList(
    [property: DataMember(Order = 1)] IReadOnlyList<RoleItem> Items,
    [property: DataMember(Order = 2)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RoleList() : this(new List<RoleItem>(), default)
    {
    }
}

/// <param name="IsAssigned">
/// Assigned to a team member now or before; such a role can no longer be renamed or deleted.
/// </param>
[DataContract]
public sealed record RoleItem(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string Name,
    [property: DataMember(Order = 3)] string Description,
    [property: DataMember(Order = 4)] bool IsAssigned)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RoleItem() : this(default, default!, default!, default)
    {
    }
}
