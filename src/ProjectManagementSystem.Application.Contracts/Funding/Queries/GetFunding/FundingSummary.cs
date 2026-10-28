using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Employees;

namespace ProjectManagementSystem.Application.Funding.Queries.GetFunding;

/// <param name="ActualCost">Sum of hours × hourly rate of every time entry; past entries keep their rate.</param>
/// <param name="RemainingFunding">Total funding minus actual cost; negative when overspent.</param>
/// <param name="UsedPercent">Actual cost of total funding, rounded to one decimal; empty without funding.</param>
/// <param name="Tranches">Newest first.</param>
/// <param name="CostByMember">Highest cost first.</param>
/// <param name="CostByWorkPackage">Highest cost first.</param>
/// <param name="Version">Version of the funding of the project; 0 before the first tranche.</param>
[DataContract]
public sealed record FundingSummary(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] string Currency,
    [property: DataMember(Order = 3)] decimal TotalFunding,
    [property: DataMember(Order = 4)] decimal ActualCost,
    [property: DataMember(Order = 5)] decimal RemainingFunding,
    [property: DataMember(Order = 6)] decimal? UsedPercent,
    [property: DataMember(Order = 7)] bool IsOverspent,
    [property: DataMember(Order = 8)] IReadOnlyList<FundingTrancheItem> Tranches,
    [property: DataMember(Order = 9)] IReadOnlyList<MemberCostItem> CostByMember,
    [property: DataMember(Order = 10)] IReadOnlyList<WorkPackageCostItem> CostByWorkPackage,
    [property: DataMember(Order = 11)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private FundingSummary() : this(
        default, default!, default, default, default, default, default, new List<FundingTrancheItem>(),
        new List<MemberCostItem>(), new List<WorkPackageCostItem>(), default)
    {
    }
}

[DataContract]
public sealed record FundingTrancheItem(
    [property: DataMember(Order = 1)] decimal Amount,
    [property: DataMember(Order = 2)] DateOnly Date,
    [property: DataMember(Order = 3)] string Reason)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private FundingTrancheItem() : this(default, default, default!)
    {
    }
}

[DataContract]
public sealed record MemberCostItem(
    [property: DataMember(Order = 1)] EmployeeRef Employee,
    [property: DataMember(Order = 2)] decimal Hours,
    [property: DataMember(Order = 3)] decimal Cost)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private MemberCostItem() : this(default!, default, default)
    {
    }
}

[DataContract]
public sealed record WorkPackageCostItem(
    [property: DataMember(Order = 1)] Guid WorkPackageId,
    [property: DataMember(Order = 2)] string Description,
    [property: DataMember(Order = 3)] decimal Hours,
    [property: DataMember(Order = 4)] decimal Cost)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private WorkPackageCostItem() : this(default, default!, default, default)
    {
    }
}
