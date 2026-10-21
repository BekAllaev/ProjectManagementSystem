using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Funding;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;

namespace ProjectManagementSystem.Application.Funding.Commands.AddFunding;

/// <summary>
/// The project is only read: it must be open. The funding of the project is created on its first tranche.
/// </summary>
public sealed class AddFundingCommandHandler(
    IRepository<Project> projects,
    IRepository<ProjectFunding> fundings,
    FundingService fundingService) : ICommandHandler<AddFundingCommand>
{
    public async Task<Unit> Handle(AddFundingCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);

        var funding = await fundings.FindAsync(project.Id, cancellationToken);
        var isNew = funding is null;
        funding ??= fundingService.CreateFunding(project);

        fundingService.AddFunding(project, funding, command.Amount, command.Date, command.Reason ?? string.Empty);

        if (isNew)
            fundings.Add(funding);
        else
            fundings.Update(funding);

        return Unit.Value;
    }
}
