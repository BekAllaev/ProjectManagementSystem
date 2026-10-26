using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.ChangeRoleDescription;

[DataContract]
public sealed record ChangeRoleDescriptionCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] Guid RoleId,
    [property: DataMember(Order = 3)] string? Description) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ChangeRoleDescriptionCommand() : this(default, default, default)
    {
    }
}
