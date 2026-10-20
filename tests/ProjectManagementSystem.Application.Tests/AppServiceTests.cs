using ProjectManagementSystem.Application.Contracts.Schedule;
using ProjectManagementSystem.Application.Schedule;
using ProjectManagementSystem.Application.Team;
using ProjectManagementSystem.Application.Tests.Fakes;
using ProjectManagementSystem.Application.TimeTracking;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Tests;

public class AppServiceTests
{
    private readonly InMemoryRepository<Project> _projects = new();
    private readonly InMemoryRepository<WorkPackage> _workPackages = new();
    private readonly InMemoryRepository<ScheduleNetwork> _networks = new();
    private readonly InMemoryRepository<TaskTimeLog> _timeLogs = new();
    private readonly InMemoryWorkPackageLookup _workPackageLookup;
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Project _project;
    private readonly WorkPackage _workPackage;

    public AppServiceTests()
    {
        _workPackageLookup = new InMemoryWorkPackageLookup(_workPackages);
        _project = TestData.ProjectWithTeam(_employeeId);
        _workPackage = TestData.WorkPackage(_project, _employeeId);
        _projects.Add(_project);
        _workPackages.Add(_workPackage);
    }

    [Fact]
    public async Task First_dependency_creates_the_network_of_the_project()
    {
        var predecessor = TestData.Task(_project, _workPackage, _employeeId);
        var successor = TestData.Task(_project, _workPackage, _employeeId);
        var otherSuccessor = TestData.Task(_project, _workPackage, _employeeId);
        var service = new ScheduleAppService(_networks, _workPackages, _workPackageLookup, new DependencyService(), _unitOfWork);

        await service.AddDependencyAsync(_project.Id, predecessor.Id, successor.Id, DependencyTypeDto.FinishToStart);
        await service.AddDependencyAsync(_project.Id, predecessor.Id, otherSuccessor.Id, DependencyTypeDto.StartToStart);

        var network = Assert.Single(_networks.All);
        Assert.Equal(_project.Id, network.ProjectId);
        Assert.Equal(2, network.Dependencies.Count);
        Assert.Equal(2, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task First_time_entry_creates_the_time_log_of_the_task()
    {
        var task = TestData.Task(_project, _workPackage, _employeeId);
        _workPackage.StartTask(task.Id);
        var service = new TimeTrackingAppService(
            _projects, _workPackages, _timeLogs, _workPackageLookup, new TimeTrackingService(), _unitOfWork);

        await service.LogTimeAsync(task.Id, _employeeId, TestData.Today, 2m);
        var entryId = await service.LogTimeAsync(task.Id, _employeeId, TestData.Today, 3m);
        await service.RemoveTimeEntryAsync(task.Id, entryId);

        var timeLog = Assert.Single(_timeLogs.All);
        Assert.Equal(task.Id, timeLog.TaskId);
        Assert.Equal(2m, Assert.Single(timeLog.Entries).Hours);
    }

    [Fact]
    public async Task Responsible_person_of_open_work_cannot_leave_the_team()
    {
        var service = new TeamMembershipAppService(
            _projects, _workPackages, _workPackageLookup, new TeamMembershipService(), _unitOfWork);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.EndTeamMembershipAsync(_project.Id, _employeeId, TestData.Today));
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
