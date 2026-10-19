using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.Funding;

/// <summary>
/// One portion of money given to the project. Amount is in the currency of the project.
/// </summary>
public sealed record FundingTranche
{
    public decimal Amount { get; }

    public DateOnly Date { get; }

    public string Reason { get; }

    public FundingTranche(decimal amount, DateOnly date, string reason)
    {
        Amount = Guard.Positive(amount, "Funding amount");
        Date = date;
        Reason = reason ?? string.Empty;
    }
}
