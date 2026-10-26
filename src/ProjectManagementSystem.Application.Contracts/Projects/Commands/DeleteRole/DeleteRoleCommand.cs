using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.DeleteRole;

[DataContract]
public sealed record DeleteRoleCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] Guid RoleId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private DeleteRoleCommand() : this(default, default)
    {
    }
}
