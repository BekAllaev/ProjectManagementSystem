using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.TimeTracking.Events;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Tests.Fakes;

namespace ProjectManagementSystem.Tests.Domain;

public class TimeTrackingServiceTests
{
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly InMemoryTaskTimeLogRepository _timeLogs = new();
    private readonly TimeTrackingService _service;
    private readonly Project _project;
    private readonly WorkPackage _workPackage;
    private readonly ProjectTask _task;

    public TimeTrackingServiceTests()
    {
        _service = new TimeTrackingService(_timeLogs);
        _project = TestData.ProjectWithTeam(_employeeId);
        _workPackage = TestData.WorkPackage(_project, _employeeId);
        _task = TestData.Task(_project, _workPackage, _employeeId);
    }

    [Fact]
    public async Task Entry_keeps_the_rate_valid_on_the_work_date()
    {
        _workPackage.StartTask(_task.Id);
        _project.ChangeHourlyRate(_employeeId, 70m, TestData.Today);

        await _service.LogTimeAsync(_project, _workPackage, _task.Id, _employeeId, TestData.Today.AddDays(-1), 2m);
        await _service.LogTimeAsync(_project, _workPackage, _task.Id, _employeeId, TestData.Today, 3m);

        var timeLog = Assert.Single(await AllLogs());
        Assert.Equal([50m, 70m], timeLog.Entries.Select(e => e.HourlyRate));
        Assert.Equal(2, timeLog.DomainEvents.OfType<TimeLogged>().Count());
    }

    [Fact]
    public async Task Time_is_logged_only_on_a_task_in_progress()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            _service.LogTimeAsync(_project, _workPackage, _task.Id, _employeeId, TestData.Today, 1m));

        _workPackage.StartTask(_task.Id);
        _workPackage.CompleteTask(_task.Id);

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.LogTimeAsync(_project, _workPackage, _task.Id, _employeeId, TestData.Today, 1m));
    }

    [Fact]
    public async Task Time_is_not_logged_for_a_future_date_or_outside_the_membership()
    {
        _workPackage.StartTask(_task.Id);

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.LogTimeAsync(_project, _workPackage, _task.Id, _employeeId, TestData.Today.AddDays(1), 1m));
        await Assert.ThrowsAsync<DomainException>(() =>
            _service.LogTimeAsync(_project, _workPackage, _task.Id, _employeeId, TestData.Today.AddDays(-31), 1m));
        await Assert.ThrowsAsync<DomainException>(() =>
            _service.LogTimeAsync(_project, _workPackage, _task.Id, Guid.NewGuid(), TestData.Today, 1m));
    }

    [Fact]
    public async Task Wrong_entry_is_removed_only_while_the_task_is_in_progress()
    {
        _workPackage.StartTask(_task.Id);
        var first = await _service.LogTimeAsync(_project, _workPackage, _task.Id, _employeeId, TestData.Today, 2m);
        var second = await _service.LogTimeAsync(_project, _workPackage, _task.Id, _employeeId, TestData.Today, 2m);
        var timeLog = Assert.Single(await AllLogs());

        _service.RemoveEntry(_workPackage, timeLog, second);

        Assert.Equal(first, Assert.Single(timeLog.Entries).Id);
        Assert.Contains(timeLog.DomainEvents, e => e is TimeEntryRemoved removed && removed.EntryId == second);

        _workPackage.CompleteTask(_task.Id);
        Assert.Throws<DomainException>(() => _service.RemoveEntry(_workPackage, timeLog, first));
    }

    private async Task<IReadOnlyList<ProjectManagementSystem.Domain.TimeTracking.TaskTimeLog>> AllLogs()
    {
        var timeLog = await _timeLogs.FindAsync(_task.Id);
        return timeLog is null ? [] : [timeLog];
    }
}
