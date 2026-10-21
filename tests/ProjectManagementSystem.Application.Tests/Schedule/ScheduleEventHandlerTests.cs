using ProjectManagementSystem.Application.Schedule.Commands.AddDependency;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Schedule.Events;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Tests.Schedule;

/// <summary>
/// WorkPackage → ScheduleNetwork synchronisation and the notification of the successor task.
/// </summary>
public class ScheduleEventHandlerTests : CommandTestBase
{
    private readonly Guid _successorOwner = Guid.NewGuid();
    private readonly Project _project;
    private readonly WorkPackage _first;
    private readonly WorkPackage _second;

    public ScheduleEventHandlerTests()
    {
        _project = SeedProject(_successorOwner);
        _first = SeedWorkPackage(_project, taskCount: 2);
        _second = TestData.WorkPackage(_project, Member);
        TestData.Task(_project, _second, _successorOwner);
        App.WorkPackages.Seed(_second);
    }

    private Guid A => _first.Tasks.First().Id;

    private Guid B => _first.Tasks.Last().Id;

    private Guid C => _second.Tasks.Single().Id;

    [Fact]
    public async Task CancelTask_WithUnfinishedSuccessor_RemovesDependencyAndNotifiesResponsiblePersons()
    {
        await App.SendAsync(Dependency(_first, A, _second, C));
        await App.SendAsync(new CancelTaskCommand(_first.Id, A));

        var delivered = await App.DeliverEventsAsync();

        Assert.Empty(App.Networks.All.Single().Dependencies);
        Assert.Single(delivered.OfType<PredecessorCancelled>());
        Assert.Equal(
            new[] { _successorOwner, Member }.Order(),
            App.Notifications.Sent.Select(n => n.RecipientEmployeeId!.Value).Order());
    }

    [Fact]
    public async Task CancelTask_SuccessorAlreadyFinished_RemovesDependencyWithoutNotification()
    {
        await App.SendAsync(Dependency(_first, A, _second, C));
        await App.SendAsync(new StartTaskCommand(_second.Id, C));
        await App.SendAsync(new CompleteTaskCommand(_second.Id, C));
        await App.SendAsync(new CancelTaskCommand(_first.Id, A));

        await App.DeliverEventsAsync();

        Assert.Empty(App.Networks.All.Single().Dependencies);
        Assert.Empty(App.Notifications.Sent);
    }

    [Fact]
    public async Task CancelWorkPackage_RemovesDependenciesOfItsCancelledTasks()
    {
        await App.SendAsync(Dependency(_first, A, _first, B));
        await App.SendAsync(Dependency(_first, B, _second, C));
        await App.SendAsync(new CancelWorkPackageCommand(_first.Id, "Out of scope"));

        var delivered = await App.DeliverEventsAsync();

        Assert.Empty(App.Networks.All.Single().Dependencies);
        Assert.Equal(2, delivered.OfType<DependencyRemoved>().Count());
        // A and B are both cancelled, so only B → C has a successor to notify about.
        Assert.Single(delivered.OfType<PredecessorCancelled>());
    }

    [Fact]
    public async Task DependencyAdded_TaskCancelledConcurrently_RemovesTheDependency()
    {
        await App.SendAsync(Dependency(_first, A, _second, C));
        var added = App.UnitOfWork.CollectedEvents.OfType<DependencyAdded>().Single();
        // The cancellation is committed, but its event is not delivered yet.
        _first.CancelTask(A);
        _first.ClearDomainEvents();

        await App.DispatchAsync(added);

        Assert.Empty(App.Networks.All.Single().Dependencies);
        Assert.Single(App.UnitOfWork.CollectedEvents.OfType<PredecessorCancelled>());
    }

    [Fact]
    public async Task Redelivery_OfTheWholeFlow_ChangesNothing()
    {
        await App.SendAsync(Dependency(_first, A, _second, C));
        await App.SendAsync(new CancelTaskCommand(_first.Id, A));
        var delivered = await App.DeliverEventsAsync();
        var eventCount = App.UnitOfWork.CollectedEvents.Count;
        var notificationCount = App.Notifications.Sent.Count;

        await App.RedeliverAsync(delivered);

        Assert.Empty(App.Networks.All.Single().Dependencies);
        Assert.Equal(eventCount, App.UnitOfWork.CollectedEvents.Count);
        Assert.Equal(notificationCount, App.Notifications.Sent.Count);
    }

    private AddDependencyCommand Dependency(WorkPackage predecessorPackage, Guid predecessor, WorkPackage successorPackage, Guid successor) =>
        new(_project.Id, predecessorPackage.Id, predecessor, successorPackage.Id, successor, DependencyType.FinishToStart);
}
