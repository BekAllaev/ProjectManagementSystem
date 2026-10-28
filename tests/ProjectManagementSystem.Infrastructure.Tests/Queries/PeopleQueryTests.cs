using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Employees.Queries.GetCurrentEmployee;
using ProjectManagementSystem.Application.Employees.Queries.GetEmployees;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Notifications;
using ProjectManagementSystem.Application.Notifications.Commands.MarkAllNotificationsRead;
using ProjectManagementSystem.Application.Notifications.Commands.MarkNotificationRead;
using ProjectManagementSystem.Application.Notifications.Queries.GetNotifications;
using ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateRole;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.Persistence.Notifications;

namespace ProjectManagementSystem.Infrastructure.Tests.Queries;

/// <summary>
/// Read models of people: employees and the notifications of the caller.
/// </summary>
public sealed class PeopleQueryTests : ReadModelTestBase
{
    [Fact]
    public async Task Employees_are_ordered_by_name_with_the_projects_they_are_on_today()
    {
        await CreateProjectAsync("Portal");
        var amira = await RegisterEmployeeAsync("Amira", "Haddad");
        var tomas = await RegisterEmployeeAsync("Tomas", "Silva");
        var lena = await RegisterEmployeeAsync("Lena", "Ortiz");
        await SendAndDeliverAsync(new AddTeamMemberCommand(ProjectId, amira, RoleId, Today.AddDays(-5), 50m));
        await SendAndDeliverAsync(new AddTeamMemberCommand(ProjectId, lena, RoleId, Today.AddDays(-50), 50m, Today.AddDays(-1)));
        await SendAndDeliverAsync(new AddTeamMemberCommand(ProjectId, tomas, RoleId, Today.AddDays(3), 50m));
        var billing = (await SendAndDeliverAsync(new CreateProjectCommand("Billing", null, "EUR"))).Id;
        var analyst = (await SendAndDeliverAsync(new CreateRoleCommand(billing, "Analyst", null))).Id;
        await SendAndDeliverAsync(new AddTeamMemberCommand(billing, amira, analyst, Today, 50m));

        var all = await SendAsync(new GetEmployeesQuery(null));
        var found = await SendAsync(new GetEmployeesQuery("SILVA"));

        Assert.Equal(["Haddad", "Ortiz", "Silva", "Weber"], all.Items.Select(e => e.LastName));
        Assert.Equal(["Billing", "Portal"], all.Items[0].CurrentProjects.Select(p => p.Title));
        Assert.Empty(all.Items[1].CurrentProjects);
        Assert.Empty(all.Items[2].CurrentProjects);
        Assert.Equal(["Portal"], all.Items[3].CurrentProjects.Select(p => p.Title));
        Assert.Equal(["tomas.silva@example.com"], found.Items.Select(e => e.Email));
    }

    [Fact]
    public async Task Current_employee_is_the_employee_of_the_caller()
    {
        var jonas = await RegisterEmployeeAsync("Jonas", "Weber");

        CurrentUser.EmployeeId = jonas;
        var current = await SendAsync(new GetCurrentEmployeeQuery());

        Assert.Equal((jonas, "Jonas", "Weber", "jonas.weber@example.com"), (current.Id, current.FirstName, current.LastName, current.Email));
        CurrentUser.EmployeeId = Guid.NewGuid();
        await Assert.ThrowsAsync<AggregateNotFoundException>(() => SendAsync(new GetCurrentEmployeeQuery()));
    }

    [Fact]
    public async Task Notifications_of_the_caller_include_those_for_the_management_of_their_projects()
    {
        await CreateProjectAsync("Portal");
        var workPackage = await CreateWorkPackageAsync("Auth");
        var task = await CreateTaskAsync(workPackage, "Pentest");
        var foreign = (await SendAndDeliverAsync(new CreateProjectCommand("Elsewhere", null, "EUR"))).Id;
        await NotifyAsync(
            Record(ProjectId, Caller, NotificationKind.PredecessorCancelled, minutesAgo: 30, workPackage, task),
            Record(ProjectId, null, NotificationKind.ResponsibleOutsideTeam, minutesAgo: 10, workPackage),
            Record(ProjectId, Guid.NewGuid(), NotificationKind.PredecessorCancelled, minutesAgo: 5),
            Record(foreign, null, NotificationKind.ResponsibleOutsideTeam, minutesAgo: 1));

        var all = await SendAsync(new GetNotificationsQuery(UnreadOnly: false, Take: null));
        var latest = await SendAsync(new GetNotificationsQuery(UnreadOnly: false, Take: 1));

        Assert.Equal([NotificationKind.ResponsibleOutsideTeam, NotificationKind.PredecessorCancelled], all.Items.Select(n => n.Kind));
        Assert.Equal([true, false], all.Items.Select(n => n.IsForProjectManagement));
        Assert.Equal(("Pentest", "Portal"), (all.Items[1].TaskName, all.Items[1].ProjectTitle));
        Assert.Null(all.Items[0].TaskName);
        Assert.Equal(2, all.UnreadCount);
        Assert.Single(latest.Items);
        Assert.Equal(2, latest.UnreadCount);
    }

    [Fact]
    public async Task Marking_a_notification_read_changes_only_that_one_and_only_once()
    {
        await CreateProjectAsync();
        var first = Record(ProjectId, Caller, NotificationKind.PredecessorCancelled, minutesAgo: 20);
        var second = Record(ProjectId, Caller, NotificationKind.PredecessorCancelled, minutesAgo: 10);
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
        await CreateProjectAsync();
        var foreign = Record(ProjectId, Guid.NewGuid(), NotificationKind.PredecessorCancelled, minutesAgo: 1);
        await NotifyAsync(foreign);

        await Assert.ThrowsAsync<AggregateNotFoundException>(() => SendAsync(new MarkNotificationReadCommand(foreign.Id)));

        Assert.Null((await StoredAsync(foreign.Id)).ReadAt);
    }

    [Fact]
    public async Task Marking_all_read_leaves_the_notifications_of_others_unread()
    {
        await CreateProjectAsync();
        var mine = Record(ProjectId, Caller, NotificationKind.PredecessorCancelled, minutesAgo: 3);
        var management = Record(ProjectId, null, NotificationKind.ResponsibleOutsideTeam, minutesAgo: 2);
        var theirs = Record(ProjectId, Guid.NewGuid(), NotificationKind.PredecessorCancelled, minutesAgo: 1);
        await NotifyAsync(mine, management, theirs);

        await SendAsync(new MarkAllNotificationsReadCommand());

        Assert.Equal(0, (await SendAsync(new GetNotificationsQuery(UnreadOnly: false, Take: null))).UnreadCount);
        Assert.NotNull((await StoredAsync(management.Id)).ReadAt);
        Assert.Null((await StoredAsync(theirs.Id)).ReadAt);
    }

    private NotificationItemView Record(
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

    private Task NotifyAsync(params NotificationItemView[] notifications) =>
        InScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            dbContext.Notifications.AddRange(notifications);
            await dbContext.SaveChangesAsync();
        });

    private async Task<NotificationItemView> StoredAsync(Guid id)
    {
        NotificationItemView? stored = null;
        await InScopeAsync(async services =>
            stored = await services.GetRequiredService<AppDbContext>().Notifications.AsNoTracking().SingleAsync(n => n.Id == id));

        return stored!;
    }
}
