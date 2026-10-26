using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;

/// <summary>
/// Completes the milestone, even if not all its tasks are done; afterwards it is frozen.
/// </summary>
[DataContract]
public sealed record CompleteMilestoneCommand(
    [property: DataMember(Order = 1)] Guid MilestoneId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CompleteMilestoneCommand() : this(Guid.Empty)
    {
    }
}
