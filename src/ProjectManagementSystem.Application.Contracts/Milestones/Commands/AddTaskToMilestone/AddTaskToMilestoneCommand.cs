using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;

[DataContract]
public sealed record AddTaskToMilestoneCommand(
    [property: DataMember(Order = 1)] Guid MilestoneId,
    [property: DataMember(Order = 2)] Guid WorkPackageId,
    [property: DataMember(Order = 3)] Guid TaskId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private AddTaskToMilestoneCommand() : this(default, default, default)
    {
    }
}
