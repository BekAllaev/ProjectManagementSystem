using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Specifications;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Operations on <see cref="WorkPackage"/> whose rules need the project: creating a work package and
/// assigning responsible persons to work packages and tasks. The person must be a member of the
/// project team at the moment of assignment; the team lives in <see cref="Project"/>, so the rule
/// spans two aggregates. Only <see cref="WorkPackage"/> is changed.
/// </summary>
public sealed class WorkPackageService(TimeProvider timeProvider)
{
    /// <summary>
    /// Creates a work package in an open project. The project is only read: the package is registered in
    /// it after the commit by the handler of <see cref="WorkPackages.Events.WorkPackageCreated"/>
    /// (<see cref="Project.RegisterWorkPackage"/>). If the project is closed in the meantime, the handler
    /// cancels the package, so a closed project never keeps an open package that
    /// <see cref="ProjectService.Complete"/> has not seen.
    /// </summary>
    /// <returns>New work package; the caller adds it to the repository.</returns>
    public WorkPackage CreateWorkPackage(Project project, string description, Guid responsiblePersonId)
    {
        if (!project.IsOpen)
            throw new DomainException($"Project '{project.Id}' is {project.State}; it gets no new work packages.");

        EnsureIsCurrentTeamMember(project, responsiblePersonId);

        return new WorkPackage(Guid.NewGuid(), project.Id, description, responsiblePersonId);
    }

    public ProjectTask AddTask(
        Project project,
        WorkPackage workPackage,
        string name,
        string description,
        Guid responsiblePersonId,
        decimal estimatedEffort)
    {
        EnsureBelongsToProject(workPackage, project);
        EnsureIsCurrentTeamMember(project, responsiblePersonId);

        return workPackage.AddTask(name, description, responsiblePersonId, estimatedEffort);
    }

    public void ChangeWorkPackageResponsiblePerson(Project project, WorkPackage workPackage, Guid employeeId)
    {
        EnsureBelongsToProject(workPackage, project);
        EnsureIsCurrentTeamMember(project, employeeId);

        workPackage.ChangeResponsiblePerson(employeeId);
    }

    public void ChangeTaskResponsiblePerson(Project project, WorkPackage workPackage, Guid taskId, Guid employeeId)
    {
        EnsureBelongsToProject(workPackage, project);
        EnsureIsCurrentTeamMember(project, employeeId);

        workPackage.ChangeTaskResponsiblePerson(taskId, employeeId);
    }

    private void EnsureIsCurrentTeamMember(Project project, Guid employeeId)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (!new IsTeamMemberOnSpecification(employeeId, today).IsSatisfiedBy(project))
            throw new DomainException($"Employee '{employeeId}' is not a member of the project team.");
    }

    private static void EnsureBelongsToProject(WorkPackage workPackage, Project project)
    {
        if (workPackage.ProjectId != project.Id)
            throw new DomainException($"Work package '{workPackage.Id}' does not belong to project '{project.Id}'.");
    }
}
