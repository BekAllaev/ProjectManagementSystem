using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Funding;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Risks;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Every aggregate is saved through its repository and the unit of work, loaded in a new scope and compared field by
/// field: decimals with their precision, dates, enums, nullable fields both empty and set, owned collections, lists of
/// ids and the version.
/// </summary>
public sealed class AggregateRoundTripTests(IntegrationTestDatabase database) : IntegrationTestBase(database)
{
    private readonly Guid _member = Guid.NewGuid();

    [SqlServerFact]
    public async Task Project()
    {
        var leaver = Guid.NewGuid();
        var workPackageIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var project = new Project(Guid.NewGuid(), "Title", "Description", new Currency("eur"), Now);
        var developer = project.CreateRole("Developer", "Writes code");
        var tester = project.CreateRole("Tester", "");
        project.AddTeamMember(_member, developer, Today.AddDays(-30), 50.25m);
        project.ChangeHourlyRate(_member, 61.5m, Today.AddDays(10), Today);
        project.AddTeamMember(leaver, tester, Today.AddDays(-20), 40m, to: Today.AddDays(5));
        foreach (var id in workPackageIds)
            project.RegisterWorkPackage(id);
        project.Start(Now.AddHours(1));

        await AddAsync(project);
        var loaded = await LoadAsync<Project>(project.Id);

        Assert.Equal("Title", loaded.Title);
        Assert.Equal("Description", loaded.Description);
        Assert.Equal("EUR", loaded.Currency.Code);
        Assert.Equal(ProjectState.InProgress, loaded.State);
        Assert.Equal(Now, loaded.CreationDate);
        Assert.Equal(Now.AddHours(1), loaded.StartDate);
        Assert.Null(loaded.CompletionDate);
        Assert.Equal(0, loaded.Version);
        Assert.Equal(workPackageIds, loaded.WorkPackageIds);

        Assert.Equal(
            [(developer, "Developer", "Writes code"), (tester, "Tester", "")],
            loaded.Roles.Select(r => (r.Id, r.Name, r.Description)).OrderBy(r => r.Name));

        var member = loaded.TeamMembers.Single(m => m.EmployeeId == _member);
        Assert.Equal((developer, Today.AddDays(-30), (DateOnly?)null), (member.RoleId, member.From, member.To));
        Assert.Equal(
            [(50.25m, Today.AddDays(-30)), (61.5m, Today.AddDays(10))],
            member.HourlyRates.Select(r => (r.Amount, r.From)));

        var left = loaded.TeamMembers.Single(m => m.EmployeeId == leaver);
        Assert.Equal((tester, Today.AddDays(-20), (DateOnly?)Today.AddDays(5)), (left.RoleId, left.From, left.To));
        Assert.Equal([(40m, Today.AddDays(-20))], left.HourlyRates.Select(r => (r.Amount, r.From)));
    }

    [SqlServerFact]
    public async Task Project_update_raises_the_version_and_keeps_the_owned_change()
    {
        var project = TestData.ProjectWithTeam(_member);
        await AddAsync(project);

        await UpdateAsync<Project>(project.Id, p => p.CreateRole("Manager", "Plans"));
        var loaded = await LoadAsync<Project>(project.Id);

        Assert.Equal(1, loaded.Version);
        Assert.Contains(loaded.Roles, r => r.Name == "Manager" && r.Description == "Plans");
    }

    [SqlServerFact]
    public async Task Completed_project()
    {
        var project = TestData.ProjectWithTeam(_member);
        TestData.Close(project);

        await AddAsync(project);
        var loaded = await LoadAsync<Project>(project.Id);

        Assert.Equal(ProjectState.Completed, loaded.State);
        Assert.Equal(project.StartDate, loaded.StartDate);
        Assert.Equal(project.CompletionDate, loaded.CompletionDate);
        Assert.NotNull(loaded.CompletionDate);
    }

    [SqlServerFact]
    public async Task WorkPackage()
    {
        var project = TestData.ProjectWithTeam(_member);
        var service = new WorkPackageService(Clock);
        var open = service.CreateWorkPackage(project, "Open package", _member);
        var started = service.AddTask(project, open, "Started", "First task", _member, estimatedEffort: 8.25m);
        var cancelled = service.AddTask(project, open, "Cancelled", "", _member, estimatedEffort: 3m);
        open.StartTask(started.Id, open.ResponsiblePersonId);
        open.CancelTask(cancelled.Id, open.ResponsiblePersonId);
        var dropped = service.CreateWorkPackage(project, "Dropped package", _member);
        dropped.Cancel(dropped.ResponsiblePersonId, "Not needed");

        await AddAsync(open, dropped);
        var loadedOpen = await LoadAsync<WorkPackage>(open.Id);
        var loadedDropped = await LoadAsync<WorkPackage>(dropped.Id);

        Assert.Equal(
            (project.Id, "Open package", _member, WorkPackageStatus.Open, (string?)null, 0L),
            (loadedOpen.ProjectId, loadedOpen.Description, loadedOpen.ResponsiblePersonId, loadedOpen.Status, loadedOpen.CancellationReason, loadedOpen.Version));
        Assert.Equal(
            [
                (started.Id, open.Id, "Started", "First task", _member, ProjectTaskStatus.InProgress, 8.25m),
                (cancelled.Id, open.Id, "Cancelled", "", _member, ProjectTaskStatus.Cancelled, 3m),
            ],
            loadedOpen.Tasks
                .Select(t => (t.Id, t.WorkPackageId, t.Name, t.Description, t.ResponsiblePersonId, t.Status, t.EstimatedEffort))
                .OrderByDescending(t => t.Name));

        Assert.Equal(WorkPackageStatus.Cancelled, loadedDropped.Status);
        Assert.Equal("Not needed", loadedDropped.CancellationReason);
        Assert.Empty(loadedDropped.Tasks);
    }

    [SqlServerFact]
    public async Task ScheduleNetwork()
    {
        var project = TestData.ProjectWithTeam(_member);
        var first = TestData.WorkPackage(project, _member);
        var second = TestData.WorkPackage(project, _member);
        var design = TestData.Task(project, first, _member);
        var build = TestData.Task(project, second, _member);
        var test = TestData.Task(project, second, _member);
        var network = new ScheduleNetwork(project.Id);
        var service = new ScheduleNetworkService();
        service.AddDependency(project, network, first, design.Id, second, build.Id, DependencyType.FinishToStart);
        service.AddDependency(project, network, second, build.Id, second, test.Id, DependencyType.StartToStart);

        await AddAsync(network);
        var loaded = await LoadAsync<ScheduleNetwork>(project.Id);

        Assert.Equal(project.Id, loaded.ProjectId);
        Assert.Equal(0, loaded.Version);
        Assert.Equal(
            new HashSet<Dependency>
            {
                new(design.Id, first.Id, build.Id, second.Id, DependencyType.FinishToStart),
                new(build.Id, second.Id, test.Id, second.Id, DependencyType.StartToStart),
            },
            loaded.Dependencies.ToHashSet());
    }

    [SqlServerFact]
    public async Task Milestone()
    {
        var project = TestData.ProjectWithTeam(_member);
        var workPackage = TestData.WorkPackage(project, _member);
        var tasks = new[] { TestData.Task(project, workPackage, _member).Id, TestData.Task(project, workPackage, _member).Id };
        var service = new MilestoneService();
        var open = service.Create(project, "Beta", "First users", Today.AddDays(30));
        foreach (var taskId in tasks)
            service.AddTask(project, open, workPackage, taskId);
        var completed = service.Create(project, "Alpha", "", Today.AddDays(-1));
        completed.Complete(Now);

        await AddAsync(open, completed);
        var loadedOpen = await LoadAsync<Milestone>(open.Id);
        var loadedCompleted = await LoadAsync<Milestone>(completed.Id);

        Assert.Equal(
            (project.Id, "Beta", "First users", Today.AddDays(30), MilestoneStatus.Open, (DateTime?)null, 0L),
            (loadedOpen.ProjectId, loadedOpen.Name, loadedOpen.Description, loadedOpen.TargetDate, loadedOpen.Status, loadedOpen.CompletedAt, loadedOpen.Version));
        Assert.Equal(tasks, loadedOpen.TaskIds);

        Assert.Equal(MilestoneStatus.Completed, loadedCompleted.Status);
        Assert.Equal(Now, loadedCompleted.CompletedAt);
        Assert.Empty(loadedCompleted.TaskIds);
    }

    [SqlServerFact]
    public async Task ProjectFunding()
    {
        var project = TestData.ProjectWithTeam(_member);
        var service = new FundingService();
        var funding = service.CreateFunding(project);
        service.AddFunding(project, funding, 1000.55m, Today, "Grant");
        service.AddFunding(project, funding, 1000.55m, Today, "Grant");
        service.AddFunding(project, funding, 250m, Today.AddDays(-1), "");

        await AddAsync(funding);
        var loaded = await LoadAsync<ProjectFunding>(project.Id);

        Assert.Equal(project.Id, loaded.ProjectId);
        Assert.Equal(0, loaded.Version);
        Assert.Equal(
            [(1000.55m, Today, "Grant"), (1000.55m, Today, "Grant"), (250m, Today.AddDays(-1), "")],
            loaded.Tranches.Select(t => (t.Amount, t.Date, t.Reason)));
    }

    [SqlServerFact]
    public async Task TaskTimeLog()
    {
        var project = TestData.ProjectWithTeam(_member);
        var workPackage = TestData.WorkPackage(project, _member);
        var task = TestData.Task(project, workPackage, _member);
        workPackage.StartTask(task.Id, workPackage.ResponsiblePersonId);
        var service = new TaskTimeLogService(Clock);
        var timeLog = service.CreateTimeLog(workPackage, task.Id);
        var today = service.LogTime(project, workPackage, timeLog, _member, Today, hours: 1.75m);
        var yesterday = service.LogTime(project, workPackage, timeLog, _member, Today.AddDays(-1), hours: 7.5m);

        await AddAsync(timeLog);
        var loaded = await LoadAsync<TaskTimeLog>(task.Id);

        Assert.Equal((task.Id, workPackage.Id, project.Id, 0L), (loaded.TaskId, loaded.WorkPackageId, loaded.ProjectId, loaded.Version));
        Assert.Equal(
            new HashSet<(Guid, Guid, DateOnly, decimal, decimal)>
            {
                (today.Id, _member, Today, 1.75m, 50m),
                (yesterday.Id, _member, Today.AddDays(-1), 7.5m, 50m),
            },
            loaded.Entries.Select(e => (e.Id, e.EmployeeId, e.WorkDate, e.Hours, e.HourlyRate)).ToHashSet());
    }

    [SqlServerFact]
    public async Task Risk()
    {
        var risk = new Risk(Guid.NewGuid(), Guid.NewGuid(), "Supplier is late", RiskLevel.High, RiskLevel.Low, _member, RiskStatus.Open);

        await AddAsync(risk);
        var loaded = await LoadAsync<Risk>(risk.Id);

        Assert.Equal(
            (risk.ProjectId, "Supplier is late", RiskLevel.High, RiskLevel.Low, _member, RiskStatus.Open, 0L),
            (loaded.ProjectId, loaded.Description, loaded.Probability, loaded.Impact, loaded.OwnerId, loaded.Status, loaded.Version));
    }

    [SqlServerFact]
    public async Task Employee()
    {
        Employee withPhoto, withoutPhoto;
        await using (var scope = Services.CreateAsyncScope())
        {
            var service = new EmployeeService(scope.ServiceProvider.GetRequiredService<IRepository<Employee>>());
            withPhoto = await service.RegisterAsync("Ada", "Lovelace", " Ada@Example.COM ", "photos/ada.png");
            withoutPhoto = await service.RegisterAsync("Alan", "Turing", "alan@example.com");
        }

        await AddAsync(withPhoto, withoutPhoto);
        var loadedWithPhoto = await LoadAsync<Employee>(withPhoto.Id);
        var loadedWithoutPhoto = await LoadAsync<Employee>(withoutPhoto.Id);

        Assert.Equal(
            ("Ada", "Lovelace", withPhoto.Email, withPhoto.NormalizedEmail, (string?)"photos/ada.png", 0L),
            (loadedWithPhoto.FirstName, loadedWithPhoto.LastName, loadedWithPhoto.Email, loadedWithPhoto.NormalizedEmail, loadedWithPhoto.PhotoPath, loadedWithPhoto.Version));
        Assert.Null(loadedWithoutPhoto.PhotoPath);
    }

    [SqlServerFact]
    public async Task Loading_raises_no_domain_events()
    {
        var project = TestData.ProjectWithTeam(_member);
        var workPackage = TestData.WorkPackage(project, _member);
        var task = TestData.Task(project, workPackage, _member);
        workPackage.StartTask(task.Id, workPackage.ResponsiblePersonId);
        var milestone = new MilestoneService().Create(project, "Beta", "", Today);
        var funding = new FundingService().CreateFunding(project);
        new FundingService().AddFunding(project, funding, 10m, Today, "");
        var timeLog = new TaskTimeLogService(Clock).CreateTimeLog(workPackage, task.Id);
        new TaskTimeLogService(Clock).LogTime(project, workPackage, timeLog, _member, Today, 1m);
        var risk = new Risk(Guid.NewGuid(), project.Id, "Risk", RiskLevel.Low, RiskLevel.Low, _member, RiskStatus.Open);

        await AddAsync(project);
        await AddAsync(workPackage);
        await AddAsync(milestone);
        await AddAsync(funding);
        await AddAsync(timeLog);
        await AddAsync(risk);
        await AddAsync(new ScheduleNetwork(project.Id));

        AggregateRoot<Guid>[] loaded =
        [
            await LoadAsync<Project>(project.Id),
            await LoadAsync<WorkPackage>(workPackage.Id),
            await LoadAsync<Milestone>(milestone.Id),
            await LoadAsync<ProjectFunding>(project.Id),
            await LoadAsync<TaskTimeLog>(task.Id),
            await LoadAsync<Risk>(risk.Id),
            await LoadAsync<ScheduleNetwork>(project.Id),
        ];

        Assert.All(loaded, aggregate => Assert.Empty(aggregate.DomainEvents));
    }
}
