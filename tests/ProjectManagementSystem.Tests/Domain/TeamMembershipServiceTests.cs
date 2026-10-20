using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Tests.Fakes;

namespace ProjectManagementSystem.Tests.Domain;

public class TeamMembershipServiceTests
{
    private readonly InMemoryWorkPackageRepository _workPackages = new();
    private readonly TeamMembershipService _service;
    private readonly Guid _lead = Guid.NewGuid();
    private readonly Guid _developer = Guid.NewGuid();

    public TeamMembershipServiceTests() => _service = new TeamMembershipService(_workPackages);

    [Fact]
    public async Task Responsible_person_of_an_open_task_cannot_leave_the_team()
    {
        var project = TestData.ProjectWithTeam(_lead, _developer);
        var workPackage = TestData.WorkPackage(project, _lead);
        TestData.Task(project, workPackage, _developer);
        _workPackages.Add(workPackage);

        await Assert.ThrowsAsync<DomainException>(() => _service.EndTeamMembershipAsync(project, _developer, TestData.Today));
        await Assert.ThrowsAsync<DomainException>(() => _service.EndTeamMembershipAsync(project, _lead, TestData.Today));
    }

    [Fact]
    public async Task Employee_can_leave_when_their_work_is_finished()
    {
        var project = TestData.ProjectWithTeam(_lead, _developer);
        var workPackage = TestData.WorkPackage(project, _lead);
        var task = TestData.Task(project, workPackage, _developer);
        workPackage.CancelTask(task.Id);
        _workPackages.Add(workPackage);

        await _service.EndTeamMembershipAsync(project, _developer, TestData.Today);

        Assert.False(project.IsTeamMemberOn(_developer, TestData.Today.AddDays(1)));
    }
}
