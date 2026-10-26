using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;

[DataContract]
public sealed record StartTaskCommand(
    [property: DataMember(Order = 1)] Guid WorkPackageId,
    [property: DataMember(Order = 2)] Guid TaskId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private StartTaskCommand() : this(default, default)
    {
    }
}
