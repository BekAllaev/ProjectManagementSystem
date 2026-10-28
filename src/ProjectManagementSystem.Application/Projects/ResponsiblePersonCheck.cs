using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Notifications;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Specifications;

namespace ProjectManagementSystem.Application.Projects;

/// <summary>
/// Re-check after the commit of the rule "the responsible person of open work is a member of the team".
/// The rule is checked before a change by <c>WorkPackageService</c> and <c>ProjectService.EndTeamMembership</c>,
/// but an assignment and leaving the team can be committed concurrently, and a membership ending in the future
/// still allows new assignments. Whichever change is committed last is detected here: on an assignment the
/// person is checked, on leaving the team all work packages of the project are checked. Nothing is changed
/// automatically; the management of the project is notified to reassign the work.
/// "Member" here means a membership without an end date, the state that <c>EndTeamMembership</c> ends.
/// The idempotency key names the work package and the person, so the two sides of a race and a repeated
/// delivery produce one notification. Used by the event handlers of the project and of the work packages.
/// </summary>
public sealed class ResponsiblePersonCheck(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    INotificationSender notificationSender)
{
    /// <summary>
    /// The employee was made responsible for work in the work package.
    /// </summary>
    public async Task CheckAssignmentAsync(Guid projectId, Guid workPackageId, Guid employeeId, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(projectId, cancellationToken);
        if (project is null || HasOngoingMembership(project, employeeId))
            return;

        var workPackage = await workPackages.FindAsync(workPackageId, cancellationToken);
        if (workPackage is not null && IsResponsibleForOpenWork(workPackage, employeeId))
            await NotifyAsync(project, workPackage, employeeId, cancellationToken);
    }

    /// <summary>
    /// The membership of the employee in the team ended or will end.
    /// </summary>
    public async Task CheckLeavingAsync(Guid projectId, Guid employeeId, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(projectId, cancellationToken);
        if (project is null || HasOngoingMembership(project, employeeId))
            return;

        foreach (var workPackageId in project.WorkPackageIds)
        {
            var workPackage = await workPackages.FindAsync(workPackageId, cancellationToken);
            if (workPackage is not null && IsResponsibleForOpenWork(workPackage, employeeId))
                await NotifyAsync(project, workPackage, employeeId, cancellationToken);
        }
    }

    private static bool HasOngoingMembership(Project project, Guid employeeId) =>
        project.IsTeamMemberOn(employeeId, DateOnly.MaxValue);

    private static bool IsResponsibleForOpenWork(WorkPackage workPackage, Guid employeeId) =>
        new IsResponsibleForOpenWorkSpecification(employeeId).IsSatisfiedBy(workPackage);

    private Task NotifyAsync(Project project, WorkPackage workPackage, Guid employeeId, CancellationToken cancellationToken) =>
        notificationSender.SendAsync(
            new Notification(
                $"responsible-outside-team:{workPackage.Id}:{employeeId}",
                project.Id,
                RecipientEmployeeId: null,
                NotificationKind.ResponsibleOutsideTeam,
                "Responsible person is no longer on the team",
                $"Employee '{employeeId}' is responsible for open work in work package '{workPackage.Id}' " +
                $"but has no ongoing membership in the team of project '{project.Title}'. Reassign the work.",
                workPackage.Id),
            cancellationToken);
}
