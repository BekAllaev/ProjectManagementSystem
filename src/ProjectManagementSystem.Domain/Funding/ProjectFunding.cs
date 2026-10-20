using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Funding.Events;

namespace ProjectManagementSystem.Domain.Funding;

/// <summary>
/// Funding of one project; its id is the id of the project. There is no budget (BAC, cost baseline): the project is planned
/// iteratively (rolling wave planning), so funding only grows and may be exceeded.
/// Amounts are in the currency of the project. Actual cost and the remaining funding are
/// computed on the read side from time entries.
/// </summary>
public sealed class ProjectFunding : AggregateRoot<Guid>
{
    private readonly List<FundingTranche> _tranches = [];

    public Guid ProjectId => Id;

    public IReadOnlyCollection<FundingTranche> Tranches => _tranches.AsReadOnly();

    public ProjectFunding(Guid projectId) : base(projectId)
    {
    }

    public void AddFunding(decimal amount, DateOnly date, string reason)
    {
        var tranche = new FundingTranche(amount, date, reason);
        _tranches.Add(tranche);

        RaiseDomainEvent(new FundingAdded(ProjectId, tranche.Amount, tranche.Date, tranche.Reason));
    }
}
