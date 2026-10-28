using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Schedule.Events;

namespace ProjectManagementSystem.Infrastructure.ReadModels.Projections;

/// <summary>
/// Keeps the dependencies of the ScheduleNetwork aggregate, with the names of both tasks and their packages, which
/// never change. Statuses and warnings are kept by the refresh of the project, which runs after every change. Every
/// event sets the version of the network on the project.
/// </summary>
internal sealed class ScheduleProjection(ReadModelStore store, ProjectReadModelRefresher refresher) :
    IDomainEventHandler<DependencyAdded>,
    IDomainEventHandler<DependencyRemoved>
{
    public async Task HandleAsync(DependencyAdded domainEvent, CancellationToken cancellationToken = default)
    {
        var dependency = domainEvent.Dependency;

        if (await FindAsync(domainEvent.ProjectId, dependency, cancellationToken) is null)
        {
            var predecessor = await store.FindAsync<TaskItemView>(cancellationToken, dependency.PredecessorTaskId);
            var successor = await store.FindAsync<TaskItemView>(cancellationToken, dependency.SuccessorTaskId);

            store.Add(new DependencyItemView
            {
                ProjectId = domainEvent.ProjectId,
                PredecessorTaskId = dependency.PredecessorTaskId,
                PredecessorWorkPackageId = dependency.PredecessorWorkPackageId,
                PredecessorName = predecessor?.Name ?? string.Empty,
                PredecessorWorkPackageDescription = predecessor?.WorkPackageDescription ?? string.Empty,
                SuccessorTaskId = dependency.SuccessorTaskId,
                SuccessorWorkPackageId = dependency.SuccessorWorkPackageId,
                SuccessorName = successor?.Name ?? string.Empty,
                SuccessorWorkPackageDescription = successor?.WorkPackageDescription ?? string.Empty,
                Type = dependency.Type,
                IsAcrossWorkPackages = dependency.PredecessorWorkPackageId != dependency.SuccessorWorkPackageId,
            });
        }

        await ChangedAsync(domainEvent.ProjectId, domainEvent, cancellationToken);
    }

    public async Task HandleAsync(DependencyRemoved domainEvent, CancellationToken cancellationToken = default)
    {
        if (await FindAsync(domainEvent.ProjectId, domainEvent.Dependency, cancellationToken) is { } row)
            store.Remove(row);

        await ChangedAsync(domainEvent.ProjectId, domainEvent, cancellationToken);
    }

    private Task<DependencyItemView?> FindAsync(Guid projectId, Dependency dependency, CancellationToken cancellationToken) =>
        store.FindAsync<DependencyItemView>(cancellationToken, projectId, dependency.PredecessorTaskId, dependency.SuccessorTaskId);

    private async Task ChangedAsync(Guid projectId, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (await store.FindAsync<ProjectItemView>(cancellationToken, projectId) is { } project)
            project.ScheduleVersion = Math.Max(project.ScheduleVersion, domainEvent.AggregateVersion);

        await refresher.RefreshAsync(projectId, cancellationToken);
    }
}
