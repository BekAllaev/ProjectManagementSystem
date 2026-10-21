using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;

public sealed class StartTaskCommandHandler(IRepository<WorkPackage> workPackages) : ICommandHandler<StartTaskCommand>
{
    public async Task<Unit> Handle(StartTaskCommand command, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.GetAsync(command.WorkPackageId, cancellationToken);

        workPackage.StartTask(command.TaskId);

        workPackages.Update(workPackage);
        return Unit.Value;
    }
}
