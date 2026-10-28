namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// A funding tranche (<c>FundingTrancheItem</c>). Tranches have no id of their own, so the row is keyed by the
/// event that added it.
/// </summary>
public sealed class FundingTrancheItemView
{
    public Guid EventId { get; set; }

    public Guid ProjectId { get; set; }

    public decimal Amount { get; set; }

    public DateOnly Date { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime AddedAt { get; set; }
}
