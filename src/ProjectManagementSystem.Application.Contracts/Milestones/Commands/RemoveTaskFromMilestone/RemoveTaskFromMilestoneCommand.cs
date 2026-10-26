using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.RemoveTaskFromMilestone;

[DataContract]
public sealed record RemoveTaskFromMilestoneCommand(
    [property: DataMember(Order = 1)] Guid MilestoneId,
    [property: DataMember(Order = 2)] Guid TaskId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RemoveTaskFromMilestoneCommand() : this(default, default)
    {
    }
}
