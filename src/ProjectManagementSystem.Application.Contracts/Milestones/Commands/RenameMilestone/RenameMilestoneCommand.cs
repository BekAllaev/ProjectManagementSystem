using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Commands.RenameMilestone;

[DataContract]
public sealed record RenameMilestoneCommand(
    [property: DataMember(Order = 1)] Guid MilestoneId,
    [property: DataMember(Order = 2)] string Name) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RenameMilestoneCommand() : this(default, default!)
    {
    }
}
