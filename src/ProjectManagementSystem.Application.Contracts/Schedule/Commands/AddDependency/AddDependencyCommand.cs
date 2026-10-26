using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Schedule;

namespace ProjectManagementSystem.Application.Schedule.Commands.AddDependency;

/// <summary>
/// Makes the successor task depend on the predecessor task. Each task is given with its work package.
/// </summary>
[DataContract]
public sealed record AddDependencyCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] Guid PredecessorWorkPackageId,
    [property: DataMember(Order = 3)] Guid PredecessorTaskId,
    [property: DataMember(Order = 4)] Guid SuccessorWorkPackageId,
    [property: DataMember(Order = 5)] Guid SuccessorTaskId,
    [property: DataMember(Order = 6)] DependencyType Type) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private AddDependencyCommand() : this(default, default, default, default, default, default)
    {
    }
}
