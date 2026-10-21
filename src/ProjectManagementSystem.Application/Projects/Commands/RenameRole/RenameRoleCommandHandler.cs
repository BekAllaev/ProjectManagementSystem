using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Commands.RenameRole;

public sealed class RenameRoleCommandHandler(IRepository<Project> projects) : ICommandHandler<RenameRoleCommand>
{
    public async Task<Unit> Handle(RenameRoleCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);

        project.RenameRole(command.RoleId, command.NewName);

        projects.Update(project);
        return Unit.Value;
    }
}
