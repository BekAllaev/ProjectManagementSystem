using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Application.Risks.Commands.RegisterRisk;

/// <summary>
/// Risk is a plain register record without domain rules; the project is only read to reject an unknown id.
/// </summary>
public sealed class RegisterRiskCommandHandler(IRepository<Project> projects, IRepository<Risk> risks)
    : ICommandHandler<RegisterRiskCommand, CreatedId>
{
    public async Task<CreatedId> Handle(RegisterRiskCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);

        var risk = new Risk(
            Guid.NewGuid(), project.Id, command.Description, command.Probability, command.Impact, command.OwnerId, RiskStatus.Open);

        risks.Add(risk);
        return new CreatedId(risk.Id);
    }
}
