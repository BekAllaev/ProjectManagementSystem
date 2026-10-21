using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;

namespace ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;

public sealed class CompleteMilestoneCommandHandler(IRepository<Milestone> milestones, TimeProvider timeProvider)
    : ICommandHandler<CompleteMilestoneCommand>
{
    public async Task<Unit> Handle(CompleteMilestoneCommand command, CancellationToken cancellationToken)
    {
        var milestone = await milestones.GetAsync(command.MilestoneId, cancellationToken);

        milestone.Complete(timeProvider.GetUtcNow().UtcDateTime);

        milestones.Update(milestone);
        return Unit.Value;
    }
}
