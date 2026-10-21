using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;

namespace ProjectManagementSystem.Application.Milestones.Commands.DeleteMilestone;

public sealed class DeleteMilestoneCommandHandler(IRepository<Milestone> milestones)
    : ICommandHandler<DeleteMilestoneCommand>
{
    public async Task<Unit> Handle(DeleteMilestoneCommand command, CancellationToken cancellationToken)
    {
        var milestone = await milestones.GetAsync(command.MilestoneId, cancellationToken);

        milestone.Delete();

        milestones.Delete(milestone);
        return Unit.Value;
    }
}
