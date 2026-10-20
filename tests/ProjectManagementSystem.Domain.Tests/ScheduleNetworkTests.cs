using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Schedule.Events;

namespace ProjectManagementSystem.Domain.Tests;

public class ScheduleNetworkTests
{
    private readonly ScheduleNetwork _network = new(Guid.NewGuid());
    private readonly Guid _a = Guid.NewGuid();
    private readonly Guid _b = Guid.NewGuid();
    private readonly Guid _c = Guid.NewGuid();

    [Fact]
    public void Dependency_that_closes_a_cycle_is_rejected()
    {
        _network.AddDependency(Link(_a, _b));
        _network.AddDependency(Link(_b, _c));

        Assert.Throws<DomainException>(() => _network.AddDependency(Link(_c, _a)));
        Assert.Equal(2, _network.Dependencies.Count);
    }

    [Fact]
    public void Removing_dependencies_of_a_cancelled_task_notifies_its_successors()
    {
        _network.AddDependency(Link(_a, _b));
        _network.AddDependency(Link(_c, _a));
        _network.ClearDomainEvents();

        _network.RemoveDependenciesOf([_a]);

        Assert.Empty(_network.Dependencies);
        Assert.Equal(2, _network.DomainEvents.OfType<DependencyRemoved>().Count());
        var notified = Assert.Single(_network.DomainEvents.OfType<PredecessorCancelled>());
        Assert.Equal(_b, notified.Dependency.SuccessorTaskId);
    }

    [Fact]
    public void Removing_dependencies_of_the_same_tasks_again_changes_nothing()
    {
        _network.AddDependency(Link(_a, _b));
        _network.RemoveDependenciesOf([_a]);
        _network.ClearDomainEvents();

        _network.RemoveDependenciesOf([_a]);

        Assert.Empty(_network.DomainEvents);
    }

    private static Dependency Link(Guid predecessor, Guid successor) =>
        new(predecessor, Guid.NewGuid(), successor, Guid.NewGuid(), DependencyType.FinishToStart);
}
