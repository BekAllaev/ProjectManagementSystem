using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Application.Risks.Commands.RegisterRisk;

/// <param name="OwnerId">Employee who owns the risk.</param>
/// <returns>Id of the new risk.</returns>
public sealed record RegisterRiskCommand(
    Guid ProjectId,
    string Description,
    RiskLevel Probability,
    RiskLevel Impact,
    Guid OwnerId) : ICommand<Guid>;
