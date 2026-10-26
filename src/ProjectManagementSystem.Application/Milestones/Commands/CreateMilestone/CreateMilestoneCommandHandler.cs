using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;

namespace ProjectManagementSystem.Application.Milestones.Commands.CreateMilestone;

/// <summary>
/// The project is only read: it must be open.
/// </summary>
public sealed class CreateMilestoneCommandHandler(
    IRepository<Project> projects,
    IRepository<Milestone> milestones,
    MilestoneService milestoneService) : ICommandHandler<CreateMilestoneCommand, CreatedId>
{
    public async Task<CreatedId> Handle(CreateMilestoneCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);

        var milestone = milestoneService.Create(project, command.Name, command.Description ?? string.Empty, command.TargetDate);

        milestones.Add(milestone);
        return new CreatedId(milestone.Id);
    }
}
