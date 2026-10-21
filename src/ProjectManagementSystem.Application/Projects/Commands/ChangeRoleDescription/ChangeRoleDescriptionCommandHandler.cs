using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Commands.ChangeRoleDescription;

public sealed class ChangeRoleDescriptionCommandHandler(IRepository<Project> projects)
    : ICommandHandler<ChangeRoleDescriptionCommand>
{
    public async Task<Unit> Handle(ChangeRoleDescriptionCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);

        project.ChangeRoleDescription(command.RoleId, command.Description ?? string.Empty);

        projects.Update(project);
        return Unit.Value;
    }
}
