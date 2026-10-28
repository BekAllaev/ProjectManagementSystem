using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Projects.EventHandlers;
using ProjectManagementSystem.Application.WorkPackages.EventHandlers;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Funding;
using ProjectManagementSystem.Domain.Funding.Events;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Milestones.Events;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Schedule.Events;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.TimeTracking.Events;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Events;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.ReadModels.Projections;

namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// Builds the read models anew from the current state of the aggregates, for data saved before the projections existed
/// or after a change of a projection. The state of every aggregate is turned into the events that lead to it and fed to
/// the same handlers the outbox feeds (only their part that keeps the read models), so the rows are the ones the events
/// would have written. The old rows are deleted first; the work is saved project by project. The handlers come from the
/// container of the scope, so the rebuilder must be resolved from that scope.
/// </summary>
public sealed class ReadModelRebuilder(AppDbContext dbContext, IServiceProvider services)
{
    public async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        await ClearAsync(cancellationToken);

        var projectIds = await dbContext.Set<Project>().AsNoTracking()
            .OrderBy(p => p.CreationDate)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        foreach (var projectId in projectIds)
        {
            await RebuildProjectAsync(projectId, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
        }
    }

    private async Task ClearAsync(CancellationToken cancellationToken)
    {
        await dbContext.Set<ProjectItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<RoleItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<TeamMemberItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<MembershipPeriodView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<HourlyRateItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<WorkPackageItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<TaskItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<DependencyItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<TimeEntryItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<MilestoneItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<MilestoneTaskItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<FundingTrancheItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<MemberCostItemView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<WorkPackageCostItemView>().ExecuteDeleteAsync(cancellationToken);
    }

    private async Task RebuildProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var store = new ReadModelStore(dbContext);
        var refresher = services.GetRequiredService<ProjectReadModelRefresher>();
        var projects = ActivatorUtilities.CreateInstance<ProjectEventHandler>(services);
        var workPackageHandler = ActivatorUtilities.CreateInstance<WorkPackageEventHandler>(services);
        var schedule = new ScheduleProjection(store, refresher);
        var time = new TimeTrackingProjection(store, refresher);
        var fundingProjection = new FundingProjection(store, refresher);
        var milestoneProjection = new MilestoneProjection(store, refresher);

        var project = await dbContext.Set<Project>().AsNoTracking().SingleAsync(p => p.Id == projectId, cancellationToken);
        var version = project.Version;
        // The events of the state carry no real moments; moments one tick apart keep the order of creation of the packages.
        var moment = project.CreationDate;
        DateTime Next() => moment = moment.AddTicks(1);

        await projects.HandleAsync(
            new ProjectCreated(project.Id, project.Title, project.Description, project.Currency.Code, project.CreationDate)
            { AggregateVersion = version }, cancellationToken);

        foreach (var role in project.Roles)
            await projects.HandleAsync(new RoleCreated(project.Id, role.Id, role.Name, role.Description) { AggregateVersion = version }, cancellationToken);

        foreach (var member in project.TeamMembers)
        {
            var rates = member.HourlyRates.OrderBy(r => r.From).ToList();
            await projects.HandleAsync(
                new TeamMemberAdded(project.Id, member.Id, member.EmployeeId, member.RoleId, member.From, member.To, rates[0].Amount)
                { AggregateVersion = version }, cancellationToken);

            foreach (var rate in rates.Skip(1))
                await projects.HandleAsync(
                    new HourlyRateChanged(project.Id, member.Id, member.EmployeeId, rate.Amount, rate.From) { AggregateVersion = version },
                    cancellationToken);
        }

        if (project.StartDate is { } startDate)
            await projects.HandleAsync(new ProjectStarted(project.Id, startDate) { AggregateVersion = version }, cancellationToken);

        if (project.CompletionDate is { } completionDate && project.State == ProjectState.Completed)
            await projects.HandleAsync(new ProjectCompleted(project.Id, completionDate) { AggregateVersion = version }, cancellationToken);

        var workPackages = await dbContext.Set<WorkPackage>().AsNoTracking().Where(w => w.ProjectId == projectId).ToListAsync(cancellationToken);
        var registered = project.WorkPackageIds.ToList();
        foreach (var workPackage in workPackages.OrderBy(w => registered.IndexOf(w.Id) is var i and >= 0 ? i : int.MaxValue))
        {
            await RebuildWorkPackageAsync(workPackageHandler, workPackage, Next, cancellationToken);

            if (registered.Contains(workPackage.Id))
                await projects.HandleAsync(
                    new WorkPackageRegistered(project.Id, workPackage.Id) { AggregateVersion = version }, cancellationToken);
        }

        if (await dbContext.Set<ScheduleNetwork>().AsNoTracking().SingleOrDefaultAsync(n => n.Id == projectId, cancellationToken) is { } network)
        {
            foreach (var dependency in network.Dependencies)
                await schedule.HandleAsync(new DependencyAdded(project.Id, dependency) { AggregateVersion = network.Version }, cancellationToken);
        }

        foreach (var timeLog in await dbContext.Set<TaskTimeLog>().AsNoTracking().Where(l => l.ProjectId == projectId).ToListAsync(cancellationToken))
        {
            foreach (var entry in timeLog.Entries)
                await time.HandleAsync(
                    new TimeLogged(project.Id, timeLog.WorkPackageId, timeLog.Id, entry.Id, entry.EmployeeId, entry.WorkDate, entry.Hours, entry.HourlyRate)
                    { AggregateVersion = timeLog.Version }, cancellationToken);
        }

        if (await dbContext.Set<ProjectFunding>().AsNoTracking().SingleOrDefaultAsync(f => f.Id == projectId, cancellationToken) is { } funding)
        {
            foreach (var tranche in funding.Tranches)
                await fundingProjection.HandleAsync(
                    new FundingAdded(project.Id, tranche.Amount, tranche.Date, tranche.Reason)
                    { AggregateVersion = funding.Version, EventId = Guid.CreateVersion7() }, cancellationToken);
        }

        foreach (var milestone in await dbContext.Set<Milestone>().AsNoTracking().Where(m => m.ProjectId == projectId).ToListAsync(cancellationToken))
            await RebuildMilestoneAsync(milestoneProjection, milestone, cancellationToken);
    }

    private static async Task RebuildWorkPackageAsync(
        WorkPackageEventHandler handler, WorkPackage workPackage, Func<DateTime> next, CancellationToken cancellationToken)
    {
        var version = workPackage.Version;

        await handler.ProjectAsync(
            new WorkPackageCreated(workPackage.ProjectId, workPackage.Id, workPackage.Description, workPackage.ResponsiblePersonId)
            { AggregateVersion = version, OccurredAt = next() }, cancellationToken);

        foreach (var task in workPackage.Tasks)
        {
            await handler.ProjectAsync(
                new TaskAdded(workPackage.ProjectId, workPackage.Id, task.Id, task.Name, task.Description, task.ResponsiblePersonId, task.EstimatedEffort)
                { AggregateVersion = version }, cancellationToken);

            switch (task.Status)
            {
                case ProjectTaskStatus.InProgress:
                    await handler.HandleAsync(new TaskStarted(workPackage.ProjectId, workPackage.Id, task.Id) { AggregateVersion = version }, cancellationToken);
                    break;
                case ProjectTaskStatus.Completed:
                    await handler.HandleAsync(new TaskCompleted(workPackage.ProjectId, workPackage.Id, task.Id) { AggregateVersion = version }, cancellationToken);
                    break;
                case ProjectTaskStatus.Cancelled:
                    await handler.ProjectAsync(new TaskCancelled(workPackage.ProjectId, workPackage.Id, task.Id) { AggregateVersion = version }, cancellationToken);
                    break;
            }
        }

        if (workPackage.Status == WorkPackageStatus.Completed)
            await handler.HandleAsync(new WorkPackageCompleted(workPackage.ProjectId, workPackage.Id) { AggregateVersion = version }, cancellationToken);
        else if (workPackage.Status == WorkPackageStatus.Cancelled)
            await handler.ProjectAsync(
                new WorkPackageCancelled(workPackage.ProjectId, workPackage.Id, workPackage.CancellationReason ?? string.Empty, [])
                { AggregateVersion = version }, cancellationToken);
    }

    private static async Task RebuildMilestoneAsync(MilestoneProjection projection, Milestone milestone, CancellationToken cancellationToken)
    {
        var version = milestone.Version;

        await projection.HandleAsync(
            new MilestoneCreated(milestone.ProjectId, milestone.Id, milestone.Name, milestone.Description, milestone.TargetDate)
            { AggregateVersion = version }, cancellationToken);

        foreach (var taskId in milestone.TaskIds)
            await projection.HandleAsync(
                new MilestoneTaskAdded(milestone.ProjectId, milestone.Id, taskId) { AggregateVersion = version }, cancellationToken);

        if (milestone.Status == MilestoneStatus.Completed && milestone.CompletedAt is { } completedAt)
            await projection.HandleAsync(new MilestoneCompleted(milestone.ProjectId, milestone.Id, completedAt) { AggregateVersion = version }, cancellationToken);
    }
}
