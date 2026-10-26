using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;

/// <summary>
/// The project is only read (it must be open, the responsible person must be in its team). The new
/// package is registered in the project after the commit by the handler of <c>WorkPackageCreated</c>.
/// </summary>
public sealed class CreateWorkPackageCommandHandler(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    WorkPackageService workPackageService) : ICommandHandler<CreateWorkPackageCommand, CreatedId>
{
    public async Task<CreatedId> Handle(CreateWorkPackageCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);

        var workPackage = workPackageService.CreateWorkPackage(project, command.Description, command.ResponsiblePersonId);

        workPackages.Add(workPackage);
        return new CreatedId(workPackage.Id);
    }
}
