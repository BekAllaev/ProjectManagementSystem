using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;
using ProjectManagementSystem.Application.Employees.Queries.GetEmployees;
using ProjectManagementSystem.Application.Funding.Commands.AddFunding;
using ProjectManagementSystem.Application.Funding.Queries.GetFunding;
using ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.CreateMilestone;
using ProjectManagementSystem.Application.Milestones.Queries.GetMilestones;
using ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;
using ProjectManagementSystem.Application.Projects.Commands.CreateRole;
using ProjectManagementSystem.Application.Projects.Commands.StartProject;
using ProjectManagementSystem.Application.Projects.Queries.GetProject;
using ProjectManagementSystem.Application.Projects.Queries.GetProjects;
using ProjectManagementSystem.Application.Projects.Queries.GetRoles;
using ProjectManagementSystem.Application.Projects.Queries.GetTeam;
using ProjectManagementSystem.Application.Schedule.Commands.AddDependency;
using ProjectManagementSystem.Application.Schedule.Queries.GetDependencies;
using ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTask;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTasks;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetWorkPackages;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.ReadModels;

namespace ProjectManagementSystem.Infrastructure.Tests.ReadModels;

public sealed class ReadModelRebuilderTests : ReadModelTestBase
{
    [Fact]
    public async Task Rebuilt_read_models_answer_every_query_as_the_projected_ones()
    {
        await ArrangeAsync();
        var projected = await AnswersAsync();

        await InScopeAsync(services => new ReadModelRebuilder(services.GetRequiredService<AppDbContext>()).RebuildAsync());

        var rebuilt = await AnswersAsync();
        Assert.Equal(projected.Count, rebuilt.Count);
        Assert.All(projected.Zip(rebuilt), pair => Assert.Equal(pair.First, pair.Second));
    }

    private async Task ArrangeAsync()
    {
        await CreateProjectAsync("Portal");
        var colleague = await RegisterEmployeeAsync("Mei", "Tanaka");
        await AddMemberAsync(colleague);
        await SendAndDeliverAsync(new CreateRoleCommand(ProjectId, "Unused", "Never assigned"));
        await SendAndDeliverAsync(new ChangeHourlyRateCommand(ProjectId, colleague, 70m, Today.AddDays(3)));
        await SendAndDeliverAsync(new StartProjectCommand(ProjectId));

        var auth = await CreateWorkPackageAsync("Auth");
        var billing = await CreateWorkPackageAsync("Billing", colleague);
        var dropped = await CreateWorkPackageAsync("Dropped");
        var login = await CreateTaskAsync(auth, "Login");
        var tokens = await CreateTaskAsync(auth, "Tokens", colleague);
        var pentest = await CreateTaskAsync(auth, "Pentest");
        var invoices = await CreateTaskAsync(billing, "Invoices", colleague);
        await CreateTaskAsync(dropped, "Push");
        await SendAndDeliverAsync(new StartTaskCommand(auth, login));
        await SendAndDeliverAsync(new CompleteTaskCommand(auth, login));
        await SendAndDeliverAsync(new StartTaskCommand(auth, tokens));
        await SendAndDeliverAsync(new CancelTaskCommand(auth, pentest));
        await SendAndDeliverAsync(new CancelWorkPackageCommand(dropped, "Out of scope"));
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, auth, login, auth, tokens, DependencyType.FinishToStart));
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, auth, tokens, billing, invoices, DependencyType.FinishToStart));

        await SendAndDeliverAsync(new AddFundingCommand(ProjectId, 500m, Today.AddDays(-5), "Budget"));
        await SendAndDeliverAsync(new AddFundingCommand(ProjectId, 100m, Today.AddDays(-5), "Extension"));
        await SendAndDeliverAsync(new LogTimeCommand(auth, tokens, Today, 2m));
        CurrentUser.EmployeeId = colleague;
        await SendAndDeliverAsync(new LogTimeCommand(auth, tokens, Today.AddDays(-1), 3m));
        CurrentUser.EmployeeId = Caller;

        var beta = (await SendAndDeliverAsync(new CreateMilestoneCommand(ProjectId, "Beta", "Pilot customers", Today.AddDays(-2)))).Id;
        await SendAndDeliverAsync(new AddTaskToMilestoneCommand(beta, auth, tokens));
        await SendAndDeliverAsync(new AddTaskToMilestoneCommand(beta, auth, login));
        var done = (await SendAndDeliverAsync(new CreateMilestoneCommand(ProjectId, "Done", null, Today.AddDays(-9)))).Id;
        await SendAndDeliverAsync(new AddTaskToMilestoneCommand(done, auth, login));
        await SendAndDeliverAsync(new CompleteMilestoneCommand(done));
    }

    /// <summary>
    /// What every query answers, as JSON, so two states of the read models compare as a whole.
    /// </summary>
    private async Task<List<string>> AnswersAsync()
    {
        var answers = new List<object>
        {
            await SendAsync(new GetProjectsQuery(null, null)),
            await SendAsync(new GetProjectQuery(ProjectId)),
            await SendAsync(new GetTeamQuery(ProjectId)),
            await SendAsync(new GetRolesQuery(ProjectId)),
            await SendAsync(new GetWorkPackagesQuery(ProjectId)),
            await SendAsync(new GetDependenciesQuery(ProjectId)),
            await SendAsync(new GetMilestonesQuery(ProjectId)),
            await SendAsync(new GetFundingQuery(ProjectId)),
            await SendAsync(new GetDashboardQuery()),
            await SendAsync(new GetEmployeesQuery(null)),
        };
        var tasks = await SendAsync(new GetTasksQuery(ProjectId, null, null, null));
        answers.Add(tasks);
        foreach (var task in tasks.Items)
            answers.Add(await SendAsync(new GetTaskQuery(task.WorkPackageId, task.TaskId)));

        return answers.Select(a => JsonSerializer.Serialize(a, a.GetType())).ToList();
    }
}
