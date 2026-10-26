using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.StartProject;

[DataContract]
public sealed record StartProjectCommand(
    [property: DataMember(Order = 1)] Guid ProjectId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private StartProjectCommand() : this(Guid.Empty)
    {
    }
}
