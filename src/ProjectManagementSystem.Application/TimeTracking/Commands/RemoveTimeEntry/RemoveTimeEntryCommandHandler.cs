using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.TimeTracking.Commands.RemoveTimeEntry;

/// <summary>
/// The work package is only read: the task must still be in progress.
/// </summary>
public sealed class RemoveTimeEntryCommandHandler(
    IRepository<WorkPackage> workPackages,
    IRepository<TaskTimeLog> timeLogs,
    TaskTimeLogService taskTimeLogService,
    ICurrentUser currentUser) : ICommandHandler<RemoveTimeEntryCommand>
{
    public async Task<Unit> Handle(RemoveTimeEntryCommand command, CancellationToken cancellationToken)
    {
        var timeLog = await timeLogs.GetAsync(command.TaskId, cancellationToken);
        var workPackage = await workPackages.GetAsync(timeLog.WorkPackageId, cancellationToken);

        taskTimeLogService.RemoveEntry(workPackage, timeLog, command.EntryId, currentUser.EmployeeId);

        timeLogs.Update(timeLog);
        return Unit.Value;
    }
}
