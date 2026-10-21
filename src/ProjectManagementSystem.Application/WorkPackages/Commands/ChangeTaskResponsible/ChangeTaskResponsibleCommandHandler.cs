using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.ChangeTaskResponsible;

/// <summary>
/// The project is only read: the responsible person must be a member of its team.
/// </summary>
public sealed class ChangeTaskResponsibleCommandHandler(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    WorkPackageService workPackageService) : ICommandHandler<ChangeTaskResponsibleCommand>
{
    public async Task<Unit> Handle(ChangeTaskResponsibleCommand command, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.GetAsync(command.WorkPackageId, cancellationToken);
        var project = await projects.GetAsync(workPackage.ProjectId, cancellationToken);

        workPackageService.ChangeTaskResponsiblePerson(project, workPackage, command.TaskId, command.EmployeeId);

        workPackages.Update(workPackage);
        return Unit.Value;
    }
}
