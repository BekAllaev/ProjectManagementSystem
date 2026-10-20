using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class TeamMembershipServiceTests
{
    private readonly TeamMembershipService _service = new();
    private readonly Guid _lead = Guid.NewGuid();
    private readonly Guid _developer = Guid.NewGuid();

    [Fact]
    public void Responsible_person_of_open_work_cannot_leave_the_team()
    {
        var project = TestData.ProjectWithTeam(_lead, _developer);
        var workPackage = TestData.WorkPackage(project, _lead);
        TestData.Task(project, workPackage, _developer);

        Assert.Throws<DomainException>(() => _service.EndTeamMembership(project, [workPackage], _developer, TestData.Today));
        Assert.Throws<DomainException>(() => _service.EndTeamMembership(project, [workPackage], _lead, TestData.Today));
    }

    [Fact]
    public void Employee_can_leave_when_their_work_is_finished()
    {
        var project = TestData.ProjectWithTeam(_lead, _developer);
        var workPackage = TestData.WorkPackage(project, _lead);
        var task = TestData.Task(project, workPackage, _developer);
        workPackage.CancelTask(task.Id);

        _service.EndTeamMembership(project, [workPackage], _developer, TestData.Today);

        Assert.False(project.IsTeamMemberOn(_developer, TestData.Today.AddDays(1)));
    }
}
