using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Specifications;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Assigns responsible persons to work packages and tasks. The person must be a member of the
/// project team at the moment of assignment; the team lives in <see cref="Project"/>, so the rule
/// spans two aggregates. Only <see cref="WorkPackage"/> is changed.
/// </summary>
public sealed class WorkAssignmentService
{
    /// <returns>New work package; the caller adds it to the repository.</returns>
    public WorkPackage CreateWorkPackage(Project project, string description, Guid responsiblePersonId)
    {
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

    private static void EnsureIsCurrentTeamMember(Project project, Guid employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!new IsTeamMemberOnSpecification(employeeId, today).IsSatisfiedBy(project))
            throw new DomainException($"Employee '{employeeId}' is not a member of the project team.");
    }

    private static void EnsureBelongsToProject(WorkPackage workPackage, Project project)
    {
        if (workPackage.ProjectId != project.Id)
            throw new DomainException($"Work package '{workPackage.Id}' does not belong to project '{project.Id}'.");
    }
}
