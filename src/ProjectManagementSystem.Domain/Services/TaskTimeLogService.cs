using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Specifications;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Operations on <see cref="TaskTimeLog"/> whose rules need the project and the work package: logging
/// and removing time on tasks. Time is logged and removed only while the task is InProgress, by a member of
/// the project team on the work date. The entry gets the hourly rate valid on the work date; rates are
/// never changed retroactively, so the rate read here cannot be replaced later. A race with completing
/// the task is accepted. Only <see cref="TaskTimeLog"/> is changed.
/// </summary>
public sealed class TaskTimeLogService(TimeProvider timeProvider)
{
    /// <summary>
    /// Creates the time log of a task before its first entry.
    /// </summary>
    /// <returns>New time log; the caller adds it to the repository.</returns>
    public TaskTimeLog CreateTimeLog(WorkPackage workPackage, Guid taskId)
    {
        var task = workPackage.GetTask(taskId);

        return new TaskTimeLog(task.Id, workPackage.Id, workPackage.ProjectId);
    }

    public TimeEntry LogTime(
        Project project,
        WorkPackage workPackage,
        TaskTimeLog timeLog,
        Guid employeeId,
        DateOnly workDate,
        decimal hours)
    {
        if (workPackage.ProjectId != project.Id)
            throw new DomainException($"Work package '{workPackage.Id}' does not belong to project '{project.Id}'.");

        EnsureTaskIsInProgress(workPackage, timeLog);

        if (workDate > DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            throw new DomainException("Time cannot be logged for a future date.");

        if (!new IsTeamMemberOnSpecification(employeeId, workDate).IsSatisfiedBy(project))
            throw new DomainException($"Employee '{employeeId}' is not a member of the project team on {workDate}.");

        var hourlyRate = project.GetHourlyRateOn(employeeId, workDate);

        return timeLog.LogTime(employeeId, workDate, hours, hourlyRate.Amount);
    }

    public void RemoveEntry(WorkPackage workPackage, TaskTimeLog timeLog, Guid entryId)
    {
        EnsureTaskIsInProgress(workPackage, timeLog);

        timeLog.RemoveEntry(entryId);
    }

    private static void EnsureTaskIsInProgress(WorkPackage workPackage, TaskTimeLog timeLog)
    {
        if (timeLog.WorkPackageId != workPackage.Id)
            throw new DomainException($"Time log of task '{timeLog.TaskId}' does not belong to work package '{workPackage.Id}'.");

        var task = workPackage.GetTask(timeLog.TaskId);

        if (task.Status != ProjectTaskStatus.InProgress)
            throw new DomainException($"Time can be changed only on a task in progress. Task '{task.Name}' is {task.Status}.");
    }
}
