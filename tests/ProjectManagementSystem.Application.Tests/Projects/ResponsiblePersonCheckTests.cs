using ProjectManagementSystem.Application.Notifications;
using ProjectManagementSystem.Application.Projects.Commands.RemoveTeamMember;
using ProjectManagementSystem.Application.WorkPackages.Commands.ChangeWorkPackageResponsible;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Tests.Projects;

/// <summary>
/// Re-check after the commit: a responsible person of open work who has left the team is reported to the
/// management of the project.
/// </summary>
public class ResponsiblePersonCheckTests : CommandTestBase
{
    private readonly Guid _leaving = Guid.NewGuid();
    private readonly Guid _staying = Guid.NewGuid();
    private readonly Project _project;
    private readonly WorkPackage _workPackage;

    public ResponsiblePersonCheckTests()
    {
        _project = SeedProject(_leaving, _staying);
        _workPackage = SeedWorkPackage(_project);
    }

    [Fact]
    public async Task Race_LeavingTheTeamBeforeNewWorkPackageIsRegistered_NotifiesManagement()
    {
        // ProjectService.EndTeamMembership sees only registered work packages, so leaving the team succeeds.
        var workPackageId = (await App.SendAsync(new CreateWorkPackageCommand(_project.Id, "Design", _leaving))).Id;
        await App.SendAsync(new RemoveTeamMemberCommand(_project.Id, _leaving, Today));

        await App.DeliverEventsAsync();

        var notification = Assert.Single(App.Notifications.Sent);
        Assert.Null(notification.RecipientEmployeeId);
        Assert.Equal(_project.Id, notification.ProjectId);
        Assert.Equal(NotificationKind.ResponsibleOutsideTeam, notification.Kind);
        Assert.Equal(workPackageId, notification.WorkPackageId);
        Assert.Null(notification.TaskId);
        Assert.Contains(workPackageId.ToString(), notification.Message);
        Assert.Contains(_leaving.ToString(), notification.Message);
        Assert.Contains(workPackageId, _project.WorkPackageIds);
    }

    [Fact]
    public async Task AssignmentAfterLeavingInTheFutureWasAnnounced_NotifiesManagementOnce()
    {
        await App.SendAsync(new RemoveTeamMemberCommand(_project.Id, _leaving, Today.AddDays(7)));
        await App.SendAsync(new ChangeWorkPackageResponsibleCommand(_workPackage.Id, _leaving));

        var delivered = await App.DeliverEventsAsync();
        await App.RedeliverAsync(delivered);

        // Both the TeamMembershipEnded and the ResponsiblePersonChanged side detect it; one notification is sent.
        Assert.Single(App.Notifications.Sent);
    }

    [Fact]
    public async Task TaskForPersonLeavingInTheFuture_NotifiesManagement()
    {
        await App.SendAsync(new RemoveTeamMemberCommand(_project.Id, _leaving, Today.AddDays(7)));
        await App.SendAsync(new CreateTaskCommand(_workPackage.Id, "Mockups", null, _leaving, 8m));

        await App.DeliverEventsAsync();

        Assert.Single(App.Notifications.Sent);
    }

    [Fact]
    public async Task ReassignedBeforeDelivery_DoesNotNotify()
    {
        await App.SendAsync(new RemoveTeamMemberCommand(_project.Id, _leaving, Today.AddDays(7)));
        await App.SendAsync(new ChangeWorkPackageResponsibleCommand(_workPackage.Id, _leaving));
        await App.SendAsync(new ChangeWorkPackageResponsibleCommand(_workPackage.Id, _staying));

        await App.DeliverEventsAsync();

        Assert.Empty(App.Notifications.Sent);
    }

    [Fact]
    public async Task AssignmentOfOngoingMember_DoesNotNotify()
    {
        await App.SendAsync(new ChangeWorkPackageResponsibleCommand(_workPackage.Id, _staying));
        await App.SendAsync(new CreateWorkPackageCommand(_project.Id, "Design", _staying));

        await App.DeliverEventsAsync();

        Assert.Empty(App.Notifications.Sent);
    }
}
