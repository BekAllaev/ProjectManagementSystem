using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;

[DataContract]
public sealed record CompleteTaskCommand(
    [property: DataMember(Order = 1)] Guid WorkPackageId,
    [property: DataMember(Order = 2)] Guid TaskId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CompleteTaskCommand() : this(default, default)
    {
    }
}
