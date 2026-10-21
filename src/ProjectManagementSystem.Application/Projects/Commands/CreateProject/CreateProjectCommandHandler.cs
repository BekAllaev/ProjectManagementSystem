using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler(IRepository<Project> projects, TimeProvider timeProvider)
    : ICommandHandler<CreateProjectCommand, Guid>
{
    public Task<Guid> Handle(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        var project = new Project(
            Guid.NewGuid(),
            command.Title,
            command.Description ?? string.Empty,
            new Currency(command.Currency),
            timeProvider.GetUtcNow().UtcDateTime);

        projects.Add(project);

        return Task.FromResult(project.Id);
    }
}
