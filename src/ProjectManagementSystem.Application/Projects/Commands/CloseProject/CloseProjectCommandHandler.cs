using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Projects.Commands.CloseProject;

/// <summary>
/// The work packages are only read; the domain service checks that they are exactly the registered ones.
/// </summary>
public sealed class CloseProjectCommandHandler(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    ProjectService projectService) : ICommandHandler<CloseProjectCommand>
{
    public async Task<Unit> Handle(CloseProjectCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);
        var packages = await workPackages.GetManyAsync(project.WorkPackageIds, cancellationToken);

        projectService.Complete(project, packages);

        projects.Update(project);
        return Unit.Value;
    }
}
