using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;

/// <summary>
/// The project is only read: the responsible person must be a member of its team.
/// </summary>
public sealed class CreateTaskCommandHandler(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    WorkPackageService workPackageService) : ICommandHandler<CreateTaskCommand, CreatedId>
{
    public async Task<CreatedId> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.GetAsync(command.WorkPackageId, cancellationToken);
        var project = await projects.GetAsync(workPackage.ProjectId, cancellationToken);

        var task = workPackageService.AddTask(
            project,
            workPackage,
            command.Name,
            command.Description ?? string.Empty,
            command.ResponsiblePersonId,
            command.EstimatedEffort);

        workPackages.Update(workPackage);
        return new CreatedId(task.Id);
    }
}
