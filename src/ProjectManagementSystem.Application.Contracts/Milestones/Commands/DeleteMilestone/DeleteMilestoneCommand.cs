using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.DeleteMilestone;

[DataContract]
public sealed record DeleteMilestoneCommand(
    [property: DataMember(Order = 1)] Guid MilestoneId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private DeleteMilestoneCommand() : this(Guid.Empty)
    {
    }
}
