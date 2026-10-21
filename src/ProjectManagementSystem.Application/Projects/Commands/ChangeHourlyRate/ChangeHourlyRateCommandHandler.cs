using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;

public sealed class ChangeHourlyRateCommandHandler(IRepository<Project> projects, TimeProvider timeProvider)
    : ICommandHandler<ChangeHourlyRateCommand>
{
    public async Task<Unit> Handle(ChangeHourlyRateCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        project.ChangeHourlyRate(command.EmployeeId, command.Amount, command.From, today);

        projects.Update(project);
        return Unit.Value;
    }
}
