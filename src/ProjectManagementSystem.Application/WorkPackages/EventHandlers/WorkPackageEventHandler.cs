using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Projects;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Application.WorkPackages.EventHandlers;

/// <summary>
/// Reacts to the events of the WorkPackage aggregate:
/// <list type="bullet">
/// <item>keeps the read models of the package and its tasks (every event sets the version of the package on it and on
/// its tasks), then refreshes the project, because a task changes the counters, progress and warnings around it;</item>
/// <item>registers a new package in its project, or cancels it if the project was closed meanwhile (compensation);</item>
/// <item>re-checks that a new responsible person is on the team (<see cref="ResponsiblePersonCheck"/>);</item>
/// <item>removes the dependencies of cancelled tasks from the schedule network.</item>
/// </list>
/// Every delivery changes at most one aggregate, and a repeated delivery changes nothing: the read models only get
/// values set, the registration and the removal of dependencies are no-ops the second time, notifications have keys.
/// </summary>
public sealed class WorkPackageEventHandler(
    IReadModelRepository<WorkPackageItemView> workPackageItems,
    IReadModelRepository<TaskItemView> taskItems,
    IReadModelRepository<ProjectItemView> projectItems,
    ProjectReadModelRefresher refresher,
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    IRepository<ScheduleNetwork> networks,
    WorkPackageService workPackageService,
    ResponsiblePersonCheck responsiblePersonCheck,
    IUnitOfWork unitOfWork) :
    IDomainEventHandler<WorkPackageCreated>,
    IDomainEventHandler<ResponsiblePersonChanged>,
    IDomainEventHandler<WorkPackageCompleted>,
    IDomainEventHandler<WorkPackageCancelled>,
    IDomainEventHandler<TaskAdded>,
    IDomainEventHandler<TaskStarted>,
    IDomainEventHandler<TaskCompleted>,
    IDomainEventHandler<TaskCancelled>
{
    public const string ProjectClosedReason = "Project was closed before the work package was registered in it.";

    public async Task HandleAsync(WorkPackageCreated domainEvent, CancellationToken cancellationToken = default)
    {
        await ProjectAsync(domainEvent, cancellationToken);
        await responsiblePersonCheck.CheckAssignmentAsync(
            domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent.ResponsiblePersonId, cancellationToken);
        await RegisterInProjectAsync(domainEvent, cancellationToken);
    }

    /// <summary>
    /// Only the change of the read models; also used to rebuild them.
    /// </summary>
    public async Task ProjectAsync(WorkPackageCreated domainEvent, CancellationToken cancellationToken = default)
    {
        var workPackage = await workPackageItems.FindAsync(w => w.Id == domainEvent.WorkPackageId, cancellationToken);
        if (workPackage is null)
        {
            workPackage = new WorkPackageItemView
            {
                Id = domainEvent.WorkPackageId,
                ProjectId = domainEvent.ProjectId,
                Status = WorkPackageStatus.Open,
                CreatedAt = domainEvent.OccurredAt,
            };
            workPackageItems.Add(workPackage);
        }

        workPackage.Description = domainEvent.Description;
        workPackage.ResponsiblePersonId = domainEvent.ResponsiblePersonId;
        await SetVersionAsync(workPackage, domainEvent, cancellationToken);
    }

    public async Task HandleAsync(ResponsiblePersonChanged domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent, async workPackage =>
        {
            if (domainEvent.TaskId is { } taskId)
            {
                if (await taskItems.FindAsync(t => t.Id == taskId, cancellationToken) is { } task)
                    task.ResponsiblePersonId = domainEvent.ResponsiblePersonId;
                return;
            }

            workPackage.ResponsiblePersonId = domainEvent.ResponsiblePersonId;
            foreach (var task in await TasksOfAsync(workPackage.Id, cancellationToken))
                task.WorkPackageResponsiblePersonId = domainEvent.ResponsiblePersonId;
        }, cancellationToken);

        await responsiblePersonCheck.CheckAssignmentAsync(
            domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent.ResponsiblePersonId, cancellationToken);
    }

    public Task HandleAsync(WorkPackageCompleted domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent, workPackage =>
        {
            workPackage.Status = WorkPackageStatus.Completed;
            return Task.CompletedTask;
        }, cancellationToken);

    public async Task HandleAsync(WorkPackageCancelled domainEvent, CancellationToken cancellationToken = default)
    {
        await ProjectAsync(domainEvent, cancellationToken);
        await RemoveDependenciesOfAsync(domainEvent.ProjectId, domainEvent.CancelledTaskIds, cancellationToken);
    }

    /// <summary>
    /// Only the change of the read models; also used to rebuild them.
    /// </summary>
    public Task ProjectAsync(WorkPackageCancelled domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent, async workPackage =>
        {
            workPackage.Status = WorkPackageStatus.Cancelled;
            workPackage.CancellationReason = domainEvent.Reason;

            // The package cancels its unfinished tasks without an event per task.
            foreach (var task in await TasksOfAsync(workPackage.Id, cancellationToken))
            {
                if (domainEvent.CancelledTaskIds.Contains(task.Id))
                    task.Status = ProjectTaskStatus.Cancelled;
            }
        }, cancellationToken);

    public async Task HandleAsync(TaskAdded domainEvent, CancellationToken cancellationToken = default)
    {
        await ProjectAsync(domainEvent, cancellationToken);
        await responsiblePersonCheck.CheckAssignmentAsync(
            domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent.ResponsiblePersonId, cancellationToken);
    }

    /// <summary>
    /// Only the change of the read models; also used to rebuild them.
    /// </summary>
    public Task ProjectAsync(TaskAdded domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent, async workPackage =>
        {
            var task = await taskItems.FindAsync(t => t.Id == domainEvent.TaskId, cancellationToken);
            if (task is null)
            {
                var project = await projectItems.FindAsync(p => p.Id == domainEvent.ProjectId, cancellationToken);
                task = new TaskItemView
                {
                    Id = domainEvent.TaskId,
                    ProjectId = domainEvent.ProjectId,
                    ProjectTitle = project?.Title ?? string.Empty,
                    WorkPackageId = workPackage.Id,
                    WorkPackageDescription = workPackage.Description,
                    WorkPackageResponsiblePersonId = workPackage.ResponsiblePersonId,
                    Status = ProjectTaskStatus.Open,
                };
                taskItems.Add(task);
            }

            task.Name = domainEvent.Name;
            task.Description = domainEvent.Description ?? string.Empty;
            task.ResponsiblePersonId = domainEvent.ResponsiblePersonId;
            task.EstimatedEffort = domainEvent.EstimatedEffort;
        }, cancellationToken);

    public Task HandleAsync(TaskStarted domainEvent, CancellationToken cancellationToken = default) =>
        SetStatusAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent.TaskId, ProjectTaskStatus.InProgress, domainEvent, cancellationToken);

    public Task HandleAsync(TaskCompleted domainEvent, CancellationToken cancellationToken = default) =>
        SetStatusAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent.TaskId, ProjectTaskStatus.Completed, domainEvent, cancellationToken);

    public async Task HandleAsync(TaskCancelled domainEvent, CancellationToken cancellationToken = default)
    {
        await ProjectAsync(domainEvent, cancellationToken);
        await RemoveDependenciesOfAsync(domainEvent.ProjectId, [domainEvent.TaskId], cancellationToken);
    }

    /// <summary>
    /// Only the change of the read models; also used to rebuild them.
    /// </summary>
    public Task ProjectAsync(TaskCancelled domainEvent, CancellationToken cancellationToken = default) =>
        SetStatusAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent.TaskId, ProjectTaskStatus.Cancelled, domainEvent, cancellationToken);

    /// <summary>
    /// Registers the new package in its project after its creation is committed (eventual consistency). If the project
    /// was closed in the meantime, the package is not registered but cancelled, which cancels its tasks with the usual
    /// events. If the project is closed concurrently with the registration, the save fails on the version of the
    /// project and the redelivery takes the compensation path.
    /// </summary>
    private async Task RegisterInProjectAsync(WorkPackageCreated domainEvent, CancellationToken cancellationToken)
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

            workPackageService.CancelInClosedProject(project, workPackage, ProjectClosedReason);
            workPackages.Update(workPackage);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Cancelled tasks do not take part in the schedule: their dependencies are removed.
    /// </summary>
    private async Task RemoveDependenciesOfAsync(Guid projectId, IReadOnlyCollection<Guid> taskIds, CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
            return;

        var network = await networks.FindAsync(projectId, cancellationToken);
        if (network is null)
            return;

        network.RemoveDependenciesOf(taskIds);
        networks.Update(network);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private Task SetStatusAsync(
        Guid projectId, Guid workPackageId, Guid taskId, ProjectTaskStatus status, IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        ChangeAsync(projectId, workPackageId, domainEvent, async _ =>
        {
            if (await taskItems.FindAsync(t => t.Id == taskId, cancellationToken) is { } task)
                task.Status = status;
        }, cancellationToken);

    private async Task ChangeAsync(
        Guid projectId, Guid workPackageId, IDomainEvent domainEvent, Func<WorkPackageItemView, Task> change, CancellationToken cancellationToken)
    {
        var workPackage = await workPackageItems.FindAsync(w => w.Id == workPackageId, cancellationToken);
        if (workPackage is null)
            return;

        await change(workPackage);
        await SetVersionAsync(workPackage, domainEvent, cancellationToken);
        await refresher.RefreshAsync(projectId, cancellationToken);
    }

    private async Task SetVersionAsync(WorkPackageItemView workPackage, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        workPackage.Version = Math.Max(workPackage.Version, domainEvent.AggregateVersion);

        foreach (var task in await TasksOfAsync(workPackage.Id, cancellationToken))
            task.WorkPackageVersion = workPackage.Version;
    }

    private Task<List<TaskItemView>> TasksOfAsync(Guid workPackageId, CancellationToken cancellationToken) =>
        taskItems.ListAsync(t => t.WorkPackageId == workPackageId, cancellationToken);
}
