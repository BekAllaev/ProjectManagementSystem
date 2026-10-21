using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;

public sealed class CancelWorkPackageCommandHandler(IRepository<WorkPackage> workPackages) : ICommandHandler<CancelWorkPackageCommand>
{
    public async Task<Unit> Handle(CancelWorkPackageCommand command, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.GetAsync(command.WorkPackageId, cancellationToken);

        workPackage.Cancel(command.Reason);

        workPackages.Update(workPackage);
        return Unit.Value;
    }
}
