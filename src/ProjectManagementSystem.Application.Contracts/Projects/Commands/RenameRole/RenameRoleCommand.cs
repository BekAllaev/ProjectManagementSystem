using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.RenameRole;

[DataContract]
public sealed record RenameRoleCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] Guid RoleId,
    [property: DataMember(Order = 3)] string NewName) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RenameRoleCommand() : this(default, default, default!)
    {
    }
}
