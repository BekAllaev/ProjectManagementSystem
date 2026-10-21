using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;

namespace ProjectManagementSystem.Application.Milestones.Commands.RenameMilestone;

public sealed class RenameMilestoneCommandHandler(IRepository<Milestone> milestones)
    : ICommandHandler<RenameMilestoneCommand>
{
    public async Task<Unit> Handle(RenameMilestoneCommand command, CancellationToken cancellationToken)
    {
        var milestone = await milestones.GetAsync(command.MilestoneId, cancellationToken);

        milestone.Rename(command.Name);

        milestones.Update(milestone);
        return Unit.Value;
    }
}
