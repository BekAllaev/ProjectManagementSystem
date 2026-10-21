using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Application.Projects.EventHandlers;

/// <summary>
/// Registers a new work package in its project after the creation is committed (eventual consistency).
/// Compensation: if the project was closed in the meantime, the package is not registered but cancelled,
/// which cancels its tasks with the usual events. Each delivery changes one aggregate: the project or the
/// work package. If the project is closed concurrently with the registration, the save fails on the version of
/// the project and the redelivery takes the compensation path. A repeated delivery changes nothing.
/// </summary>
public sealed class RegisterWorkPackageInProject(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    IUnitOfWork unitOfWork) : IDomainEventHandler<WorkPackageCreated>
{
    public const string ProjectClosedReason = "Project was closed before the work package was registered in it.";

    public async Task HandleAsync(WorkPackageCreated domainEvent, CancellationToken cancellationToken = default)
    {
        var project = await projects.GetAsync(domainEvent.ProjectId, cancellationToken);

        if (project.WorkPackageIds.Contains(domainEvent.WorkPackageId))
            return;

        if (project.IsOpen)
        {
            project.RegisterWorkPackage(domainEvent.WorkPackageId);
            projects.Update(project);
        }
        else
        {
            var workPackage = await workPackages.GetAsync(domainEvent.WorkPackageId, cancellationToken);
            if (workPackage.Status != WorkPackageStatus.Open)
                return;

            workPackage.Cancel(ProjectClosedReason);
            workPackages.Update(workPackage);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
