using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;

/// <summary>
/// The work package and the project are only read. The time log of the task is created on its first entry;
/// the entry keeps the hourly rate valid on the work date. The time is logged for the current user.
/// </summary>
public sealed class LogTimeCommandHandler(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    IRepository<TaskTimeLog> timeLogs,
    TaskTimeLogService taskTimeLogService,
    ICurrentUser currentUser) : ICommandHandler<LogTimeCommand, Guid>
{
    public async Task<Guid> Handle(LogTimeCommand command, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.GetAsync(command.WorkPackageId, cancellationToken);
        var project = await projects.GetAsync(workPackage.ProjectId, cancellationToken);

        var timeLog = await timeLogs.FindAsync(command.TaskId, cancellationToken);
        var isNew = timeLog is null;
        timeLog ??= taskTimeLogService.CreateTimeLog(workPackage, command.TaskId);

        var entry = taskTimeLogService.LogTime(
            project, workPackage, timeLog, currentUser.EmployeeId, command.WorkDate, command.Hours);

        if (isNew)
            timeLogs.Add(timeLog);
        else
            timeLogs.Update(timeLog);

        return entry.Id;
    }
}
