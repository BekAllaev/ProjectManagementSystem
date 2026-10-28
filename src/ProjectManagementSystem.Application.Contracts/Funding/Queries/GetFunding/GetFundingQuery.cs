using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Funding.Queries.GetFunding;

/// <summary>
/// Funding of a project and its actual cost from the logged time.
/// </summary>
[DataContract]
public sealed record GetFundingQuery(
    [property: DataMember(Order = 1)] Guid ProjectId) : IQuery<FundingSummary>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetFundingQuery() : this(Guid.Empty)
    {
    }
}
