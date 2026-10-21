using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Projects.Commands.RemoveTeamMember;

/// <summary>
/// The work packages are only read: an employee responsible for open work cannot leave the team.
/// </summary>
public sealed class RemoveTeamMemberCommandHandler(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    ProjectService projectService) : ICommandHandler<RemoveTeamMemberCommand>
{
    public async Task<Unit> Handle(RemoveTeamMemberCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);
        var packages = await workPackages.GetManyAsync(project.WorkPackageIds, cancellationToken);

        projectService.EndTeamMembership(project, packages, command.EmployeeId, command.To);

        projects.Update(project);
        return Unit.Value;
    }
}
