using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTask;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.WorkPackages;

/// <summary>
/// The row of the task, its dependencies and its time entries. What the caller may do depends on the caller and on
/// the day, so it is decided on the read.
/// </summary>
internal sealed class GetTaskQueryHandler(AppDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    : IQueryHandler<GetTaskQuery, TaskDetails>
{
    public async Task<TaskDetails> Handle(GetTaskQuery query, CancellationToken cancellationToken)
    {
        var task = await dbContext.TaskItems([query.TaskId]).SingleOrDefaultAsync(cancellationToken);
        if (task is null || task.WorkPackageId != query.WorkPackageId)
            throw new EntityNotFoundException(nameof(ProjectTask), query.TaskId);

        var project = await dbContext.GetProjectItemAsync(task.ProjectId, cancellationToken);
        var dependencies = await dbContext.DependencyItemsOfTask(project.Id, task.Id).ToListAsync(cancellationToken);
        var entries = (await dbContext.TimeEntryItemsOf(task.Id).ToListAsync(cancellationToken)).OrderByDescending(e => e.WorkDate).ToList();
        var employees = await EmployeeDirectory.LoadAsync(
            dbContext, entries.Select(e => e.EmployeeId).Append(task.ResponsiblePersonId), cancellationToken);

        var me = currentUser.EmployeeId;
        var isInProgress = task.Status == ProjectTaskStatus.InProgress;
        var isMemberToday = await dbContext.MembershipPeriodsOn(timeProvider.Today(), me).AnyAsync(p => p.ProjectId == project.Id, cancellationToken);

        return new TaskDetails(
            task.Id, task.Name, task.Description, project.Id, task.WorkPackageId, task.WorkPackageDescription,
            employees.Ref(task.ResponsiblePersonId), task.Status, task.EstimatedEffort, task.HasUnfinishedFinishToStartPredecessor,
            ReadModelRules.CanChangeStatus(task, me),
            isInProgress && isMemberToday,
            dependencies
                .Where(d => d.SuccessorTaskId == task.Id)
                .Select(d => new TaskDependencyItem(
                    d.PredecessorTaskId, d.PredecessorWorkPackageId, d.PredecessorName, d.PredecessorWorkPackageDescription,
                    d.IsAcrossWorkPackages, d.PredecessorStatus, d.Type))
                .ToList(),
            dependencies
                .Where(d => d.PredecessorTaskId == task.Id)
                .Select(d => new TaskDependencyItem(
                    d.SuccessorTaskId, d.SuccessorWorkPackageId, d.SuccessorName, d.SuccessorWorkPackageDescription,
                    d.IsAcrossWorkPackages, d.SuccessorStatus, d.Type))
                .ToList(),
            entries
                .Select(e => new TimeEntryItem(
                    e.Id, employees.Ref(e.EmployeeId), e.WorkDate, e.Hours, e.HourlyRate, e.Cost, isInProgress && e.EmployeeId == me))
                .ToList(),
            task.LoggedHours, task.LoggedCost, project.Currency, task.WorkPackageVersion, task.TimeLogVersion);
    }
}
