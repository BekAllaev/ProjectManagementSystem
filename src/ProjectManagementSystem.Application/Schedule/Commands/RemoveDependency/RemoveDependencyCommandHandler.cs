using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule;

namespace ProjectManagementSystem.Application.Schedule.Commands.RemoveDependency;

public sealed class RemoveDependencyCommandHandler(IRepository<ScheduleNetwork> networks)
    : ICommandHandler<RemoveDependencyCommand>
{
    public async Task<Unit> Handle(RemoveDependencyCommand command, CancellationToken cancellationToken)
    {
        var network = await networks.GetAsync(command.ProjectId, cancellationToken);

        network.RemoveDependency(command.PredecessorTaskId, command.SuccessorTaskId);

        networks.Update(network);
        return Unit.Value;
    }
}
