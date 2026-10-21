using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Creates milestones and adds tasks to them. A milestone is created only in an open project, and a task
/// is added only if it exists in a work package of the same open project and is not cancelled; tasks live
/// in work packages, so the rule spans aggregates. A concurrent cancellation of the task (or closing of
/// the project) is accepted without compensation: the read side already ignores cancelled tasks.
/// Only <see cref="Milestone"/> is changed.
/// </summary>
public sealed class MilestoneService
{
    /// <returns>New milestone; the caller adds it to the repository.</returns>
    public Milestone Create(Project project, string name, string description, DateOnly targetDate)
    {
        EnsureProjectIsOpen(project);

        return new Milestone(Guid.NewGuid(), project.Id, name, description, targetDate);
    }

    /// <summary>
    /// A completed task may be added, a cancelled one may not.
    /// </summary>
    public void AddTask(Project project, Milestone milestone, WorkPackage workPackage, Guid taskId)
    {
        if (milestone.ProjectId != project.Id)
            throw new DomainException($"Milestone '{milestone.Id}' does not belong to project '{project.Id}'.");

        if (workPackage.ProjectId != project.Id)
            throw new DomainException($"Work package '{workPackage.Id}' does not belong to project '{project.Id}'.");

        EnsureProjectIsOpen(project);

        var task = workPackage.GetTask(taskId);
        if (task.Status == ProjectTaskStatus.Cancelled)
            throw new DomainException($"Cancelled task '{task.Name}' cannot be added to a milestone.");

        milestone.AddTask(taskId);
    }

    private static void EnsureProjectIsOpen(Project project)
    {
        if (!project.IsOpen)
            throw new DomainException($"Project '{project.Id}' is {project.State}; it gets no new milestones or milestone tasks.");
    }
}
