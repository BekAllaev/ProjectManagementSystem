using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;

public sealed class CancelTaskCommandHandler(IRepository<WorkPackage> workPackages, ICurrentUser currentUser) : ICommandHandler<CancelTaskCommand>
{
    public async Task<Unit> Handle(CancelTaskCommand command, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.GetAsync(command.WorkPackageId, cancellationToken);

        workPackage.CancelTask(command.TaskId, currentUser.EmployeeId);

        workPackages.Update(workPackage);
        return Unit.Value;
    }
}
