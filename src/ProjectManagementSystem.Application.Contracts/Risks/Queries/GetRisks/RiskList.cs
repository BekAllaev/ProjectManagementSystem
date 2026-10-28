using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Application.Risks.Queries.GetRisks;

[DataContract]
public sealed record RiskList(
    [property: DataMember(Order = 1)] IReadOnlyList<RiskItem> Items)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RiskList() : this(new List<RiskItem>())
    {
    }
}

/// <param name="Score">Probability × impact, each counted 1 (low) to 3 (high).</param>
/// <param name="Level">High from a score of 6, medium from 3, low below.</param>
/// <param name="Version">Version of the aggregate it is read from (see the read side in docs/domain-model.md).</param>
[DataContract]
public sealed record RiskItem(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string Description,
    [property: DataMember(Order = 3)] RiskLevel Probability,
    [property: DataMember(Order = 4)] RiskLevel Impact,
    [property: DataMember(Order = 5)] RiskLevel Level,
    [property: DataMember(Order = 6)] int Score,
    [property: DataMember(Order = 7)] EmployeeRef Owner,
    [property: DataMember(Order = 8)] RiskStatus Status,
    [property: DataMember(Order = 9)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RiskItem() : this(default, default!, default, default, default, default, default!, default, default)
    {
    }
}
