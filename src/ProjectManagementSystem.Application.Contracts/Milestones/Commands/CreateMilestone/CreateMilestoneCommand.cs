using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.CreateMilestone;

/// <returns>Id of the new milestone.</returns>
[DataContract]
public sealed record CreateMilestoneCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] string Name,
    [property: DataMember(Order = 3)] string? Description,
    [property: DataMember(Order = 4)] DateOnly TargetDate) : ICommand<CreatedId>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CreateMilestoneCommand() : this(default, default!, default, default)
    {
    }
}
