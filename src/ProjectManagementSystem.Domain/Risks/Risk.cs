using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Risks;

/// <summary>
/// Entry of the risk register (PMBOK: individual project risk).
/// It is a deliberate plain CRUD record without invariants and domain behaviour:
/// risks take no part in the rules of the rest of the domain, so this supporting part
/// does not need a rich model.
/// </summary>
public sealed class Risk : AggregateRoot<Guid>
{
    public Guid ProjectId { get; }

    public string Description { get; set; }

    public RiskLevel Probability { get; set; }

    public RiskLevel Impact { get; set; }

    /// <summary>
    /// Employee who owns the risk.
    /// </summary>
    public Guid OwnerId { get; set; }

    public RiskStatus Status { get; set; }

    /// <summary>
    /// For EF Core materialization only; does not raise domain events.
    /// </summary>
    private Risk()
    {
        Description = null!;
    }

    public Risk(Guid id, Guid projectId, string description, RiskLevel probability, RiskLevel impact, Guid ownerId, RiskStatus status)
        : base(id)
    {
        ProjectId = projectId;
        Description = description;
        Probability = probability;
        Impact = impact;
        OwnerId = ownerId;
        Status = status;
    }
}
