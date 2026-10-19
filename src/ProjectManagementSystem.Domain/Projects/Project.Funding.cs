using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Projects.Funding;

namespace ProjectManagementSystem.Domain.Projects;

/// <summary>
/// Funding of the project. There is no budget (BAC, cost baseline): the project is planned
/// iteratively (rolling wave planning), so funding only grows and may be exceeded.
/// </summary>
public sealed partial class Project
{
    private readonly List<FundingTranche> _fundingTranches = [];

    public Currency Currency { get; }

    public IReadOnlyCollection<FundingTranche> FundingTranches => _fundingTranches.AsReadOnly();

    public void AddFunding(decimal amount, DateOnly date, string reason)
    {
        var tranche = new FundingTranche(amount, date, reason);
        _fundingTranches.Add(tranche);

        RaiseDomainEvent(new FundingAdded(Id, tranche.Amount, Currency.Code, tranche.Date, tranche.Reason));
    }
}
