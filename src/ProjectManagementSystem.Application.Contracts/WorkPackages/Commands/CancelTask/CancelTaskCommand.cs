using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;

[DataContract]
public sealed record CancelTaskCommand(
    [property: DataMember(Order = 1)] Guid WorkPackageId,
    [property: DataMember(Order = 2)] Guid TaskId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CancelTaskCommand() : this(default, default)
    {
    }
}
