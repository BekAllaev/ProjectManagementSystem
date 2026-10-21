using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Commands.StartProject;

public sealed class StartProjectCommandHandler(IRepository<Project> projects, TimeProvider timeProvider)
    : ICommandHandler<StartProjectCommand>
{
    public async Task<Unit> Handle(StartProjectCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);

        project.Start(timeProvider.GetUtcNow().UtcDateTime);

        projects.Update(project);
        return Unit.Value;
    }
}
