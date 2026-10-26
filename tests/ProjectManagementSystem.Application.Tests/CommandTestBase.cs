using ProjectManagementSystem.Application.Tests.Fakes;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Tests;

/// <summary>
/// Pipeline-style command tests: commands go through the real <c>AddApplication()</c>, aggregates are
/// arranged with domain methods and seeded into the fake repositories.
/// </summary>
public abstract class CommandTestBase : IDisposable
{
    protected TestApplication App { get; } = new();

    /// <summary>
    /// Team member of every seeded project (since 30 days ago, hourly rate 50).
    /// </summary>
    protected Guid Member { get; } = Guid.NewGuid();

    /// <summary>
    /// <see cref="Member"/> is the caller of every request unless a test changes <c>App.CurrentUser</c>.
    /// </summary>
    protected CommandTestBase() => App.CurrentUser.EmployeeId = Member;

    protected DateTime Now => App.TimeProvider.GetUtcNow().UtcDateTime;

    protected DateOnly Today => DateOnly.FromDateTime(Now);

    protected Project SeedProject(params Guid[] additionalMembers) =>
        App.Projects.Seed(TestData.ProjectWithTeam([Member, .. additionalMembers]));

    /// <summary>
    /// Work package registered in the project (as after the handler of <c>WorkPackageCreated</c>) with
    /// <paramref name="taskCount"/> open tasks, all with <see cref="Member"/> as the responsible person.
    /// </summary>
    protected WorkPackage SeedWorkPackage(Project project, int taskCount = 0)
    {
        var workPackage = TestData.WorkPackage(project, Member);
        for (var i = 0; i < taskCount; i++)
            TestData.Task(project, workPackage, Member);

        return App.WorkPackages.Seed(workPackage);
    }

    protected async Task<Employee> SeedEmployeeAsync(string email)
    {
        var employee = await new EmployeeService(App.Employees).RegisterAsync("Ada", "Lovelace", email);

        return App.Employees.Seed(employee);
    }

    /// <summary>
    /// The command was saved once, changed only <paramref name="changedAggregate"/> and raised exactly these events.
    /// </summary>
    protected void AssertCommitted(AggregateRoot<Guid> changedAggregate, params Type[] eventTypes)
    {
        Assert.Equal(1, App.UnitOfWork.SaveCount);
        Assert.Same(changedAggregate, Assert.Single(App.UnitOfWork.ChangedAggregates));
        Assert.Equal(eventTypes, App.UnitOfWork.CollectedEvents.Select(e => e.GetType()));
    }

    /// <summary>
    /// The command failed with <typeparamref name="TException"/>, nothing was saved and no aggregate has new events.
    /// </summary>
    protected async Task<TException> AssertRejectedAsync<TException>(Func<Task> send) where TException : Exception
    {
        var exception = await Assert.ThrowsAsync<TException>(send);

        Assert.Equal(0, App.UnitOfWork.SaveCount);
        Assert.Empty(App.UnitOfWork.CollectedEvents);
        Assert.All(AllAggregates(), aggregate => Assert.Empty(aggregate.DomainEvents));

        return exception;
    }

    public void Dispose()
    {
        App.Dispose();
        GC.SuppressFinalize(this);
    }

    private IEnumerable<AggregateRoot<Guid>> AllAggregates() =>
        App.Projects.All.Cast<AggregateRoot<Guid>>()
            .Concat(App.WorkPackages.All)
            .Concat(App.Networks.All)
            .Concat(App.TimeLogs.All)
            .Concat(App.Milestones.All)
            .Concat(App.Fundings.All)
            .Concat(App.Risks.All)
            .Concat(App.Employees.All);
}
