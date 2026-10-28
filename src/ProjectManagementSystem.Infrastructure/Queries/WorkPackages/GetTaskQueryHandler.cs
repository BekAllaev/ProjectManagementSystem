using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTask;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.ReadModels;

namespace ProjectManagementSystem.Infrastructure.Queries.WorkPackages;

internal sealed class GetTaskQueryHandler(AppDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    : IQueryHandler<GetTaskQuery, TaskDetails>
{
    public async Task<TaskDetails> Handle(GetTaskQuery query, CancellationToken cancellationToken)
    {
        var workPackage = await dbContext.WorkPackages([query.WorkPackageId]).SingleOrDefaultAsync(cancellationToken)
            ?? throw new AggregateNotFoundException(nameof(WorkPackage), query.WorkPackageId);
        var task = workPackage.GetTask(query.TaskId);

        var project = await dbContext.GetProjectAsync(workPackage.ProjectId, cancellationToken);
        var network = await dbContext.Network(project.Id).SingleOrDefaultAsync(cancellationToken);
        var timeLog = await dbContext.TimeLog(task.Id).SingleOrDefaultAsync(cancellationToken);

        var dependencies = network?.Dependencies.Where(d => d.PredecessorTaskId == task.Id || d.SuccessorTaskId == task.Id).ToList() ?? [];
        var otherWorkPackageIds = dependencies
            .SelectMany(d => new[] { d.PredecessorWorkPackageId, d.SuccessorWorkPackageId })
            .Where(id => id != workPackage.Id)
            .Distinct()
            .ToList();
        var related = (await dbContext.WorkPackages(otherWorkPackageIds).ToListAsync(cancellationToken))
            .Append(workPackage)
            .ToDictionary(w => w.Id);

        var entries = timeLog?.Entries.OrderByDescending(e => e.WorkDate).ToList() ?? [];
        var employees = await EmployeeDirectory.LoadAsync(
            dbContext, entries.Select(e => e.EmployeeId).Append(task.ResponsiblePersonId), cancellationToken);
        var isInProgress = task.Status == ProjectTaskStatus.InProgress;
        var me = currentUser.EmployeeId;

        TaskDependencyItem? Item(Guid taskId, Guid workPackageId, DependencyType type)
        {
            if (!related.TryGetValue(workPackageId, out var owner) || !owner.ContainsTask(taskId))
                return null;

            var other = owner.GetTask(taskId);
            return new TaskDependencyItem(other.Id, owner.Id, other.Name, owner.Description, owner.Id != workPackage.Id, other.Status, type);
        }

        var predecessors = dependencies
            .Where(d => d.SuccessorTaskId == task.Id)
            .Select(d => Item(d.PredecessorTaskId, d.PredecessorWorkPackageId, d.Type))
            .OfType<TaskDependencyItem>()
            .ToList();
        var successors = dependencies
            .Where(d => d.PredecessorTaskId == task.Id)
            .Select(d => Item(d.SuccessorTaskId, d.SuccessorWorkPackageId, d.Type))
            .OfType<TaskDependencyItem>()
            .ToList();

        return new TaskDetails(
            task.Id,
            task.Name,
            task.Description,
            project.Id,
            workPackage.Id,
            workPackage.Description,
            employees.Ref(task.ResponsiblePersonId),
            task.Status,
            task.EstimatedEffort,
            ReadModelRules.HasUnfinishedFinishToStartPredecessor(
                task.Id, task.Status, dependencies, predecessors.ToDictionary(p => p.TaskId, p => p.Status)),
            !task.IsFinished && (task.ResponsiblePersonId == me || workPackage.ResponsiblePersonId == me),
            isInProgress && project.IsTeamMemberOn(me, timeProvider.Today()),
            predecessors,
            successors,
            entries.Select(e => new TimeEntryItem(
                e.Id, employees.Ref(e.EmployeeId), e.WorkDate, e.Hours, e.HourlyRate, e.Hours * e.HourlyRate,
                isInProgress && e.EmployeeId == me)).ToList(),
            entries.Sum(e => e.Hours),
            entries.Sum(e => e.Hours * e.HourlyRate),
            project.Currency.Code,
            workPackage.Version,
            timeLog?.Version ?? 0);
    }
}
