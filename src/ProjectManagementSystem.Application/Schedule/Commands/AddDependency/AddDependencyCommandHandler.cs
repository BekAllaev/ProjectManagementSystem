using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Schedule.Commands.AddDependency;

/// <summary>
/// The project and the work packages are only read. The network of the project is created on its first
/// dependency.
/// </summary>
public sealed class AddDependencyCommandHandler(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    IRepository<ScheduleNetwork> networks,
    ScheduleNetworkService scheduleNetworkService) : ICommandHandler<AddDependencyCommand>
{
    public async Task<Unit> Handle(AddDependencyCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);
        var predecessorWorkPackage = await workPackages.GetAsync(command.PredecessorWorkPackageId, cancellationToken);
        var successorWorkPackage = await workPackages.GetAsync(command.SuccessorWorkPackageId, cancellationToken);

        var network = await networks.FindAsync(project.Id, cancellationToken);
        var isNew = network is null;
        network ??= new ScheduleNetwork(project.Id);

        scheduleNetworkService.AddDependency(
            project,
            network,
            predecessorWorkPackage,
            command.PredecessorTaskId,
            successorWorkPackage,
            command.SuccessorTaskId,
            command.Type);

        if (isNew)
            networks.Add(network);
        else
            networks.Update(network);

        return Unit.Value;
    }
}
