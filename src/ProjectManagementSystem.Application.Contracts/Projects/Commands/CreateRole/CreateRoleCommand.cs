using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.CreateRole;

/// <returns>Id of the new role.</returns>
[DataContract]
public sealed record CreateRoleCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] string Name,
    [property: DataMember(Order = 3)] string? Description) : ICommand<CreatedId>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CreateRoleCommand() : this(default, default!, default)
    {
    }
}
