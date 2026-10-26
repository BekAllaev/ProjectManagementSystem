using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Commands.CreateRole;

public sealed class CreateRoleCommandHandler(IRepository<Project> projects) : ICommandHandler<CreateRoleCommand, CreatedId>
{
    public async Task<CreatedId> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);

        var roleId = project.CreateRole(command.Name, command.Description ?? string.Empty);

        projects.Update(project);
        return new CreatedId(roleId);
    }
}
