using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler(IRepository<Project> projects) : ICommandHandler<DeleteRoleCommand>
{
    public async Task<Unit> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);

        project.DeleteRole(command.RoleId);

        projects.Update(project);
        return Unit.Value;
    }
}
