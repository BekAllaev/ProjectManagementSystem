using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Schedule.Commands.RemoveDependency;

[DataContract]
public sealed record RemoveDependencyCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] Guid PredecessorTaskId,
    [property: DataMember(Order = 3)] Guid SuccessorTaskId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RemoveDependencyCommand() : this(default, default, default)
    {
    }
}
