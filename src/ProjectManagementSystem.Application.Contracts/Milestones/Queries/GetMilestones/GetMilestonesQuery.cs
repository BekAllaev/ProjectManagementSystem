using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Milestones.Queries.GetMilestones;

/// <summary>
/// Milestones of a project, ordered by target date.
/// </summary>
[DataContract]
public sealed record GetMilestonesQuery(
    [property: DataMember(Order = 1)] Guid ProjectId) : IQuery<MilestoneList>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetMilestonesQuery() : this(Guid.Empty)
    {
    }
}
