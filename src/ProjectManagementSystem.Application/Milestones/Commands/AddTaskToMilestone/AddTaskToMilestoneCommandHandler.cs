using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;

/// <summary>
/// The project and the work package are only read: the project must be open, the task must exist and must not
/// be cancelled.
/// </summary>
public sealed class AddTaskToMilestoneCommandHandler(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    IRepository<Milestone> milestones,
    MilestoneService milestoneService) : ICommandHandler<AddTaskToMilestoneCommand>
{
    public async Task<Unit> Handle(AddTaskToMilestoneCommand command, CancellationToken cancellationToken)
    {
        var milestone = await milestones.GetAsync(command.MilestoneId, cancellationToken);
        var project = await projects.GetAsync(milestone.ProjectId, cancellationToken);
        var workPackage = await workPackages.GetAsync(command.WorkPackageId, cancellationToken);

        milestoneService.AddTask(project, milestone, workPackage, command.TaskId);

        milestones.Update(milestone);
        return Unit.Value;
    }
}
