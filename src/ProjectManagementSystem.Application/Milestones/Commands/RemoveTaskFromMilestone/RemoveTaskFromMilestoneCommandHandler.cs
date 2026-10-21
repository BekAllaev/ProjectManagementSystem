using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;

namespace ProjectManagementSystem.Application.Milestones.Commands.RemoveTaskFromMilestone;

public sealed class RemoveTaskFromMilestoneCommandHandler(IRepository<Milestone> milestones)
    : ICommandHandler<RemoveTaskFromMilestoneCommand>
{
    public async Task<Unit> Handle(RemoveTaskFromMilestoneCommand command, CancellationToken cancellationToken)
    {
        var milestone = await milestones.GetAsync(command.MilestoneId, cancellationToken);

        milestone.RemoveTask(command.TaskId);

        milestones.Update(milestone);
        return Unit.Value;
    }
}
