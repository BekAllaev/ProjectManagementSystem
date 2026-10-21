using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Application.Risks.Commands.UpdateRisk;

/// <summary>
/// Risk is a deliberate CRUD record without domain methods, so its properties are set directly.
/// </summary>
public sealed class UpdateRiskCommandHandler(IRepository<Risk> risks) : ICommandHandler<UpdateRiskCommand>
{
    public async Task<Unit> Handle(UpdateRiskCommand command, CancellationToken cancellationToken)
    {
        var risk = await risks.GetAsync(command.RiskId, cancellationToken);

        risk.Description = command.Description;
        risk.Probability = command.Probability;
        risk.Impact = command.Impact;
        risk.OwnerId = command.OwnerId;
        risk.Status = command.Status;

        risks.Update(risk);
        return Unit.Value;
    }
}
