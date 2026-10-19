using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.Risks;

/// <summary>
/// Entry of the risk register (PMBOK: individual project risk).
/// It is a deliberate plain CRUD record without invariants and domain behaviour:
/// risks take no part in the rules of the rest of the domain, so this supporting part
/// does not need a rich model.
/// </summary>
public sealed class Risk : Entity<Guid>
{
    public string Description { get; set; }

    public RiskLevel Probability { get; set; }

    public RiskLevel Impact { get; set; }

    /// <summary>
    /// Employee who owns the risk.
    /// </summary>
    public Guid OwnerId { get; set; }

    public RiskStatus Status { get; set; }

    internal Risk(Guid id, string description, RiskLevel probability, RiskLevel impact, Guid ownerId, RiskStatus status)
        : base(id)
    {
        Description = description;
        Probability = probability;
        Impact = impact;
        OwnerId = ownerId;
        Status = status;
    }
}
