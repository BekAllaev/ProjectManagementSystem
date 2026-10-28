using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Application.Risks.Queries.GetRisks;

/// <summary>
/// Risk register of a project: open risks first, then by score, highest first.
/// </summary>
/// <param name="Status">Only risks in this status; empty for all.</param>
[DataContract]
public sealed record GetRisksQuery(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] RiskStatus? Status) : IQuery<RiskList>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetRisksQuery() : this(default, default)
    {
    }
}
