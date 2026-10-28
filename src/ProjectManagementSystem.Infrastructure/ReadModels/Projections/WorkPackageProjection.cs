using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Infrastructure.ReadModels.Projections;

/// <summary>
/// Keeps the read models of the WorkPackage aggregate: the row of the package and the rows of its tasks. Every event
/// sets the version of the package on it and on its tasks; then the project is refreshed, because a task changes the
/// counters, progress and warnings around it. Only values are set, so a repeated delivery changes nothing.
/// </summary>
internal sealed class WorkPackageProjection(ReadModelStore store, ProjectReadModelRefresher refresher) :
    IDomainEventHandler<WorkPackageCreated>,
    IDomainEventHandler<ResponsiblePersonChanged>,
    IDomainEventHandler<WorkPackageCompleted>,
    IDomainEventHandler<WorkPackageCancelled>,
    IDomainEventHandler<TaskAdded>,
    IDomainEventHandler<TaskStarted>,
    IDomainEventHandler<TaskCompleted>,
    IDomainEventHandler<TaskCancelled>
{
    public async Task HandleAsync(WorkPackageCreated domainEvent, CancellationToken cancellationToken = default)
    {
        var workPackage = await store.FindAsync<WorkPackageItemView>(cancellationToken, domainEvent.WorkPackageId);
        if (workPackage is null)
        {
            workPackage = new WorkPackageItemView
            {
                Id = domainEvent.WorkPackageId,
                ProjectId = domainEvent.ProjectId,
                Status = WorkPackageStatus.Open,
                CreatedAt = domainEvent.OccurredAt,
            };
            store.Add(workPackage);
        }

        workPackage.Description = domainEvent.Description;
        workPackage.ResponsiblePersonId = domainEvent.ResponsiblePersonId;
        await SetVersionAsync(workPackage, domainEvent, cancellationToken);
    }

    public Task HandleAsync(ResponsiblePersonChanged domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent, async workPackage =>
        {
            if (domainEvent.TaskId is { } taskId)
            {
                if (await store.FindAsync<TaskItemView>(cancellationToken, taskId) is { } task)
                    task.ResponsiblePersonId = domainEvent.ResponsiblePersonId;
                return;
            }

            workPackage.ResponsiblePersonId = domainEvent.ResponsiblePersonId;
            foreach (var task in await TasksOfAsync(workPackage.Id, cancellationToken))
                task.WorkPackageResponsiblePersonId = domainEvent.ResponsiblePersonId;
        }, cancellationToken);

    public Task HandleAsync(WorkPackageCompleted domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent, workPackage =>
        {
            workPackage.Status = WorkPackageStatus.Completed;
            return Task.CompletedTask;
        }, cancellationToken);

    public Task HandleAsync(WorkPackageCancelled domainEvent, CancellationToken cancellationToken = default) =>
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

    public Task HandleAsync(TaskAdded domainEvent, CancellationToken cancellationToken = default) =>
        ChangeAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent, async workPackage =>
        {
            var task = await store.FindAsync<TaskItemView>(cancellationToken, domainEvent.TaskId);
            if (task is null)
            {
                var project = await store.FindAsync<ProjectItemView>(cancellationToken, domainEvent.ProjectId);
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
                store.Add(task);
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

    public Task HandleAsync(TaskCancelled domainEvent, CancellationToken cancellationToken = default) =>
        SetStatusAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent.TaskId, ProjectTaskStatus.Cancelled, domainEvent, cancellationToken);

    private Task SetStatusAsync(
        Guid projectId, Guid workPackageId, Guid taskId, ProjectTaskStatus status, IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        ChangeAsync(projectId, workPackageId, domainEvent, async _ =>
        {
            if (await store.FindAsync<TaskItemView>(cancellationToken, taskId) is { } task)
                task.Status = status;
        }, cancellationToken);

    private async Task ChangeAsync(
        Guid projectId, Guid workPackageId, IDomainEvent domainEvent, Func<WorkPackageItemView, Task> change, CancellationToken cancellationToken)
    {
        var workPackage = await store.FindAsync<WorkPackageItemView>(cancellationToken, workPackageId);
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
        store.LoadAsync<TaskItemView>(t => t.WorkPackageId == workPackageId, cancellationToken);
}
