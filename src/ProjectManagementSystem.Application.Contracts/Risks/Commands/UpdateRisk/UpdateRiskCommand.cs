using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Application.Risks.Commands.UpdateRisk;

/// <summary>
/// Replaces all editable fields of the risk (plain CRUD).
/// </summary>
public sealed record UpdateRiskCommand(
    Guid RiskId,
    string Description,
    RiskLevel Probability,
    RiskLevel Impact,
    Guid OwnerId,
    RiskStatus Status) : ICommand;
