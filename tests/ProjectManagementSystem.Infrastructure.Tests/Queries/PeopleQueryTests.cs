using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;
using ProjectManagementSystem.Application.Employees.Queries.GetCurrentEmployee;
using ProjectManagementSystem.Application.Employees.Queries.GetEmployees;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Notifications;
using ProjectManagementSystem.Application.Notifications.Commands.MarkAllNotificationsRead;
using ProjectManagementSystem.Application.Notifications.Commands.MarkNotificationRead;
using ProjectManagementSystem.Application.Notifications.Queries.GetNotifications;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.Persistence.Notifications;

namespace ProjectManagementSystem.Infrastructure.Tests.Queries;

/// <summary>
/// Read models of people: employees, the dashboard and the notifications of the caller.
/// </summary>
public sealed class PeopleQueryTests : QueryTestBase
{
    [Fact]
    public async Task Employees_are_ordered_by_name_with_the_projects_they_are_on_today()
    {
        var amira = await EmployeeAsync("Amira", "Haddad");
        var tomas = await EmployeeAsync("Tomas", "Silva");
        var lena = await EmployeeAsync("Lena", "Ortiz");
        var portal = new Project(Guid.NewGuid(), "Portal", "", new Currency("EUR"), TestData.Now);
        var role = portal.CreateRole("Developer", "");
        portal.AddTeamMember(amira.Id, role, Today.AddDays(-5), 50m);
        portal.AddTeamMember(lena.Id, role, Today.AddDays(-50), 50m, to: Today.AddDays(-1));
        portal.AddTeamMember(tomas.Id, role, Today.AddDays(3), 50m);
        var billing = new Project(Guid.NewGuid(), "Billing", "", new Currency("EUR"), TestData.Now);
        billing.AddTeamMember(amira.Id, billing.CreateRole("Analyst", ""), Today, 50m);
        await StoreAsync(amira, tomas, lena, portal, billing);

        var all = await SendAsync(new GetEmployeesQuery(null));
        var found = await SendAsync(new GetEmployeesQuery("SILVA"));

        Assert.Equal(["Haddad", "Ortiz", "Silva"], all.Items.Select(e => e.LastName));
        Assert.Equal(["Billing", "Portal"], all.Items[0].CurrentProjects.Select(p => p.Title));
        Assert.Empty(all.Items[1].CurrentProjects);
        Assert.Empty(all.Items[2].CurrentProjects);
        Assert.Equal(["tomas.silva@example.com"], found.Items.Select(e => e.Email));
    }

    [Fact]
    public async Task Current_employee_is_the_employee_of_the_caller()
    {
        var jonas = await EmployeeAsync("Jonas", "Weber");
        await StoreAsync(jonas);

        CurrentUser.EmployeeId = jonas.Id;
        var current = await SendAsync(new GetCurrentEmployeeQuery());

        Assert.Equal((jonas.Id, "Jonas", "Weber", "jonas.weber@example.com"), (current.Id, current.FirstName, current.LastName, current.Email));
        CurrentUser.EmployeeId = Guid.NewGuid();
        await Assert.ThrowsAsync<AggregateNotFoundException>(() => SendAsync(new GetCurrentEmployeeQuery()));
    }

    [Fact]
    public async Task Dashboard_shows_the_open_tasks_projects_and_funding_alerts_of_the_caller()
    {
        var colleague = Guid.NewGuid();
        var portal = TestData.ProjectWithTeam(Me, colleague);
        var workPackage = TestData.WorkPackage(portal, Me);
        var mine = AddTask(portal, workPackage, "Mine", Me);
        var running = AddTask(portal, workPackage, "Running", Me);
        AddTask(portal, workPackage, "Theirs", colleague);
        var done = AddTask(portal, workPackage, "Done", Me);
        workPackage.StartTask(running.Id, Me);
        workPackage.StartTask(done.Id, Me);
        workPackage.CompleteTask(done.Id, Me);
        var fundingService = new FundingService();
        var funding = fundingService.CreateFunding(portal);
        fundingService.AddFunding(portal, funding, 100m, Today, "Budget");
        var timeLogs = new TaskTimeLogService(TestData.TimeProvider);
        var timeLog = timeLogs.CreateTimeLog(workPackage, running.Id);
        timeLogs.LogTime(portal, workPackage, timeLog, Me, Today, 1.75m);
        var healthy = TestData.ProjectWithTeam(Me);
        var notMine = TestData.ProjectWithTeam(colleague);
        await StoreAsync(portal, workPackage, funding, timeLog, healthy, notMine);

        var dashboard = await SendAsync(new GetDashboardQuery());

        Assert.Equal(["Running", "Mine"], dashboard.OpenTasks.Select(t => t.Name));
        Assert.Equal([1.75m, 0m], dashboard.OpenTasks.Select(t => t.LoggedHours));
        Assert.Equal(portal.Id, dashboard.OpenTasks[0].ProjectId);
        Assert.Equal(2, dashboard.Projects.Count);
        Assert.Equal(25, dashboard.Projects.Single(p => p.Id == portal.Id).ProgressPercent);
        // 1.75 h at the rate 50 of 100 funding: 87.5% used, above the alert threshold of 80%.
        var alert = Assert.Single(dashboard.FundingAlerts);
        Assert.Equal((portal.Id, 87.5m, 12.5m, 87.5m, false), (alert.ProjectId, alert.ActualCost, alert.RemainingFunding, alert.UsedPercent, alert.IsOverspent));
        Assert.DoesNotContain(dashboard.Projects, p => p.Id == notMine.Id);
    }

    [Fact]
    public async Task Notifications_of_the_caller_include_those_for_the_management_of_their_projects()
    {
        var portal = TestData.ProjectWithTeam(Me);
        var workPackage = TestData.WorkPackage(portal, Me);
        var task = AddTask(portal, workPackage, "Pentest", Me);
        var foreign = TestData.ProjectWithTeam(Guid.NewGuid());
        await StoreAsync(portal, workPackage, foreign);
        await NotifyAsync(
            Record(portal.Id, Me, NotificationKind.PredecessorCancelled, minutesAgo: 30, workPackage.Id, task.Id),
            Record(portal.Id, null, NotificationKind.ResponsibleOutsideTeam, minutesAgo: 10, workPackage.Id),
            Record(portal.Id, Guid.NewGuid(), NotificationKind.PredecessorCancelled, minutesAgo: 5),
            Record(foreign.Id, null, NotificationKind.ResponsibleOutsideTeam, minutesAgo: 1));

        var all = await SendAsync(new GetNotificationsQuery(UnreadOnly: false, Take: null));
        var latest = await SendAsync(new GetNotificationsQuery(UnreadOnly: false, Take: 1));

        Assert.Equal([NotificationKind.ResponsibleOutsideTeam, NotificationKind.PredecessorCancelled], all.Items.Select(n => n.Kind));
        Assert.Equal([true, false], all.Items.Select(n => n.IsForProjectManagement));
        Assert.Equal(("Pentest", "Project"), (all.Items[1].TaskName, all.Items[1].ProjectTitle));
        Assert.Null(all.Items[0].TaskName);
        Assert.Equal(2, all.UnreadCount);
        Assert.Single(latest.Items);
        Assert.Equal(2, latest.UnreadCount);
    }

    [Fact]
    public async Task Marking_a_notification_read_changes_only_that_one_and_only_once()
    {
        var portal = TestData.ProjectWithTeam(Me);
        await StoreAsync(portal);
        var first = Record(portal.Id, Me, NotificationKind.PredecessorCancelled, minutesAgo: 20);
        var second = Record(portal.Id, Me, NotificationKind.PredecessorCancelled, minutesAgo: 10);
        await NotifyAsync(first, second);

        await SendAsync(new MarkNotificationReadCommand(first.Id));
        Clock.Advance(TimeSpan.FromHours(1));
        await SendAsync(new MarkNotificationReadCommand(first.Id));

        var unread = await SendAsync(new GetNotificationsQuery(UnreadOnly: true, Take: null));
        Assert.Equal([second.Id], unread.Items.Select(n => n.Id));
        Assert.Equal(1, unread.UnreadCount);
        Assert.Equal(TestData.Now, (await StoredAsync(first.Id)).ReadAt);
    }

    [Fact]
    public async Task Notification_of_someone_else_is_not_found_and_stays_unread()
    {
        var portal = TestData.ProjectWithTeam(Me);
        await StoreAsync(portal);
        var foreign = Record(portal.Id, Guid.NewGuid(), NotificationKind.PredecessorCancelled, minutesAgo: 1);
        await NotifyAsync(foreign);

        await Assert.ThrowsAsync<AggregateNotFoundException>(() => SendAsync(new MarkNotificationReadCommand(foreign.Id)));

        Assert.Null((await StoredAsync(foreign.Id)).ReadAt);
    }

    [Fact]
    public async Task Marking_all_read_leaves_the_notifications_of_others_unread()
    {
        var portal = TestData.ProjectWithTeam(Me);
        await StoreAsync(portal);
        var mine = Record(portal.Id, Me, NotificationKind.PredecessorCancelled, minutesAgo: 3);
        var management = Record(portal.Id, null, NotificationKind.ResponsibleOutsideTeam, minutesAgo: 2);
        var theirs = Record(portal.Id, Guid.NewGuid(), NotificationKind.PredecessorCancelled, minutesAgo: 1);
        await NotifyAsync(mine, management, theirs);

        await SendAsync(new MarkAllNotificationsReadCommand());

        Assert.Equal(0, (await SendAsync(new GetNotificationsQuery(UnreadOnly: false, Take: null))).UnreadCount);
        Assert.NotNull((await StoredAsync(management.Id)).ReadAt);
        Assert.Null((await StoredAsync(theirs.Id)).ReadAt);
    }

    private NotificationRecord Record(
        Guid projectId, Guid? recipientId, NotificationKind kind, int minutesAgo, Guid? workPackageId = null, Guid? taskId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            RecipientEmployeeId = recipientId,
            Kind = kind,
            Title = kind.ToString(),
            Message = "Details",
            WorkPackageId = workPackageId,
            TaskId = taskId,
            CreatedAt = TestData.Now.AddMinutes(-minutesAgo),
        };

    private Task NotifyAsync(params NotificationRecord[] notifications) =>
        InScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            dbContext.Notifications.AddRange(notifications);
            await dbContext.SaveChangesAsync();
        });

    private async Task<NotificationRecord> StoredAsync(Guid id)
    {
        NotificationRecord? stored = null;
        await InScopeAsync(async services =>
            stored = await services.GetRequiredService<AppDbContext>().Notifications.AsNoTracking().SingleAsync(n => n.Id == id));

        return stored!;
    }
}
