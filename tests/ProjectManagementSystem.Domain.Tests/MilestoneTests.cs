using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Milestones.Events;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class MilestoneTests
{
    [Fact]
    public void Completed_milestone_is_frozen()
    {
        var milestone = new Milestone(Guid.NewGuid(), Guid.NewGuid(), "Release", "First release", TestData.Today);
        milestone.AddTask(Guid.NewGuid());

        milestone.Complete(DateTime.UtcNow);

        Assert.Equal(MilestoneStatus.Completed, milestone.Status);
        Assert.NotNull(milestone.CompletedAt);
        Assert.Throws<DomainException>(() => milestone.AddTask(Guid.NewGuid()));
        Assert.Throws<DomainException>(() => milestone.RemoveTask(milestone.TaskIds.Single()));
        Assert.Throws<DomainException>(() => milestone.ChangeTargetDate(TestData.Today.AddDays(1)));
    }

    [Fact]
    public void Delete_is_allowed_in_any_status()
    {
        var milestone = new Milestone(Guid.NewGuid(), Guid.NewGuid(), "Release", "First release", TestData.Today);
        milestone.Complete(DateTime.UtcNow);

        milestone.Delete();

        Assert.Contains(milestone.DomainEvents, e => e is MilestoneDeleted);
    }
}
