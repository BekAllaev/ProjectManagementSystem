using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CompleteWorkPackage;

public sealed class CompleteWorkPackageCommandHandler(IRepository<WorkPackage> workPackages, ICurrentUser currentUser) : ICommandHandler<CompleteWorkPackageCommand>
{
    public async Task<Unit> Handle(CompleteWorkPackageCommand command, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.GetAsync(command.WorkPackageId, cancellationToken);

        workPackage.Complete(currentUser.EmployeeId);

        workPackages.Update(workPackage);
        return Unit.Value;
    }
}
