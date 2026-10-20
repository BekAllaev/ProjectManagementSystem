using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Specifications;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Logs time on tasks. Time is logged and removed only while the task is InProgress, by a member of
/// the project team on the work date. The entry gets the hourly rate valid on the work date; rates are
/// never changed retroactively, so the rate read here cannot be replaced later. A race with completing
/// the task is accepted. Only <see cref="TaskTimeLog"/> is changed.
/// </summary>
public sealed class TimeTrackingService(ITaskTimeLogRepository timeLogs)
{
    /// <returns>Id of the new time entry.</returns>
    public async Task<Guid> LogTimeAsync(
        Project project,
        WorkPackage workPackage,
        Guid taskId,
        Guid employeeId,
        DateOnly workDate,
        decimal hours,
        CancellationToken cancellationToken = default)
    {
        if (workPackage.ProjectId != project.Id)
            throw new DomainException($"Work package '{workPackage.Id}' does not belong to project '{project.Id}'.");

        EnsureTaskIsInProgress(workPackage, taskId);

        if (workDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("Time cannot be logged for a future date.");

        if (!new IsTeamMemberOnSpecification(employeeId, workDate).IsSatisfiedBy(project))
            throw new DomainException($"Employee '{employeeId}' is not a member of the project team on {workDate}.");

        var hourlyRate = project.GetHourlyRateOn(employeeId, workDate);

        var timeLog = await timeLogs.FindAsync(taskId, cancellationToken);
        if (timeLog is null)
        {
            timeLog = new TaskTimeLog(taskId, workPackage.Id, project.Id);
            timeLogs.Add(timeLog);
        }

        return timeLog.LogTime(employeeId, workDate, hours, hourlyRate.Amount).Id;
    }

    public void RemoveEntry(WorkPackage workPackage, TaskTimeLog timeLog, Guid entryId)
    {
        if (timeLog.WorkPackageId != workPackage.Id)
            throw new DomainException($"Time log of task '{timeLog.TaskId}' does not belong to work package '{workPackage.Id}'.");

        EnsureTaskIsInProgress(workPackage, timeLog.TaskId);

        timeLog.RemoveEntry(entryId);
    }

    private static void EnsureTaskIsInProgress(WorkPackage workPackage, Guid taskId)
    {
        var task = workPackage.GetTask(taskId);

        if (task.Status != ProjectTaskStatus.InProgress)
            throw new DomainException($"Time can be changed only on a task in progress. Task '{task.Name}' is {task.Status}.");
    }
}
