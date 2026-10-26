using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.ChangeMilestoneTargetDate;

[DataContract]
public sealed record ChangeMilestoneTargetDateCommand(
    [property: DataMember(Order = 1)] Guid MilestoneId,
    [property: DataMember(Order = 2)] DateOnly TargetDate) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ChangeMilestoneTargetDateCommand() : this(default, default)
    {
    }
}
