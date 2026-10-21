using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;

public sealed class CompleteTaskCommandHandler(IRepository<WorkPackage> workPackages) : ICommandHandler<CompleteTaskCommand>
{
    public async Task<Unit> Handle(CompleteTaskCommand command, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.GetAsync(command.WorkPackageId, cancellationToken);

        workPackage.CompleteTask(command.TaskId);

        workPackages.Update(workPackage);
        return Unit.Value;
    }
}
