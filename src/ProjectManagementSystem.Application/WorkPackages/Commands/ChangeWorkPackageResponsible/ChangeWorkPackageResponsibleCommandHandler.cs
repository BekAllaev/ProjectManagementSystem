using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.ChangeWorkPackageResponsible;

/// <summary>
/// The project is only read: the responsible person must be a member of its team.
/// </summary>
public sealed class ChangeWorkPackageResponsibleCommandHandler(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    WorkPackageService workPackageService) : ICommandHandler<ChangeWorkPackageResponsibleCommand>
{
    public async Task<Unit> Handle(ChangeWorkPackageResponsibleCommand command, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.GetAsync(command.WorkPackageId, cancellationToken);
        var project = await projects.GetAsync(workPackage.ProjectId, cancellationToken);

        workPackageService.ChangeWorkPackageResponsiblePerson(project, workPackage, command.EmployeeId);

        workPackages.Update(workPackage);
        return Unit.Value;
    }
}
