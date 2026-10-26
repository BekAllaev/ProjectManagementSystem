using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.CloseProject;

/// <summary>
/// Completes the project; every registered work package must be finished.
/// </summary>
[DataContract]
public sealed record CloseProjectCommand(
    [property: DataMember(Order = 1)] Guid ProjectId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CloseProjectCommand() : this(Guid.Empty)
    {
    }
}
