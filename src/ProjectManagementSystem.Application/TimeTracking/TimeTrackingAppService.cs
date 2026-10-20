using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Contracts.TimeTracking;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.TimeTracking;

public sealed class TimeTrackingAppService(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    IRepository<TaskTimeLog> timeLogs,
    IWorkPackageLookup workPackageLookup,
    TimeTrackingService timeTrackingService,
    IUnitOfWork unitOfWork) : ITimeTrackingAppService
{
    /// <summary>
    /// The time log of the task is created on its first entry.
    /// </summary>
    public async Task<Guid> LogTimeAsync(
        Guid taskId,
        Guid employeeId,
        DateOnly workDate,
        decimal hours,
        CancellationToken cancellationToken = default)
    {
        var timeLog = await timeLogs.FindAsync(taskId, cancellationToken);
        var workPackage = await workPackages.GetAsync(
            timeLog?.WorkPackageId ?? await FindWorkPackageIdOfTaskAsync(taskId, cancellationToken),
            cancellationToken);
        var project = await projects.GetAsync(workPackage.ProjectId, cancellationToken);

        var isNew = timeLog is null;
        timeLog ??= timeTrackingService.CreateTimeLog(workPackage, taskId);

        var entry = timeTrackingService.LogTime(project, workPackage, timeLog, employeeId, workDate, hours);

        if (isNew)
            timeLogs.Add(timeLog);
        else
            timeLogs.Update(timeLog);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }

    public async Task RemoveTimeEntryAsync(Guid taskId, Guid entryId, CancellationToken cancellationToken = default)
    {
        var timeLog = await timeLogs.GetAsync(taskId, cancellationToken);
        var workPackage = await workPackages.GetAsync(timeLog.WorkPackageId, cancellationToken);

        timeTrackingService.RemoveEntry(workPackage, timeLog, entryId);

        timeLogs.Update(timeLog);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid> FindWorkPackageIdOfTaskAsync(Guid taskId, CancellationToken cancellationToken) =>
        await workPackageLookup.FindWorkPackageIdOfTaskAsync(taskId, cancellationToken)
        ?? throw new EntityNotFoundException("Task", taskId);
}
