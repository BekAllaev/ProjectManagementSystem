using Microsoft.Extensions.Time.Testing;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.TimeTracking.Events;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Tests;

public class TaskTimeLogServiceTests
{
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly TaskTimeLogService _service = new(TestData.TimeProvider);
    private readonly Project _project;
    private readonly WorkPackage _workPackage;
    private readonly ProjectTask _task;
    private readonly TaskTimeLog _timeLog;

    public TaskTimeLogServiceTests()
    {
        _project = TestData.ProjectWithTeam(_employeeId);
        _workPackage = TestData.WorkPackage(_project, _employeeId);
        _task = TestData.Task(_project, _workPackage, _employeeId);
        _timeLog = _service.CreateTimeLog(_workPackage, _task.Id);
    }

    [Fact]
    public void Time_log_belongs_to_the_task_its_package_and_project()
    {
        Assert.Equal(_task.Id, _timeLog.Id);
        Assert.Equal(_workPackage.Id, _timeLog.WorkPackageId);
        Assert.Equal(_project.Id, _timeLog.ProjectId);
        Assert.Throws<EntityNotFoundException>(() => _service.CreateTimeLog(_workPackage, Guid.NewGuid()));
    }

    [Fact]
    public void Entry_keeps_the_rate_valid_on_the_work_date()
    {
        _workPackage.StartTask(_task.Id);
        _project.ChangeHourlyRate(_employeeId, 70m, TestData.Today, TestData.Today);

        LogTime(TestData.Today.AddDays(-1), 2m);
        LogTime(TestData.Today, 3m);

        Assert.Equal([50m, 70m], _timeLog.Entries.Select(e => e.HourlyRate));
        Assert.Equal(2, _timeLog.DomainEvents.OfType<TimeLogged>().Count());
    }

    [Fact]
    public void Time_is_logged_only_on_a_task_in_progress()
    {
        Assert.Throws<DomainException>(() => LogTime(TestData.Today, 1m));

        _workPackage.StartTask(_task.Id);
        _workPackage.CompleteTask(_task.Id);

        Assert.Throws<DomainException>(() => LogTime(TestData.Today, 1m));
    }

    [Fact]
    public void Time_is_not_logged_for_a_future_date_or_outside_the_membership()
    {
        _workPackage.StartTask(_task.Id);

        Assert.Throws<DomainException>(() => LogTime(TestData.Today.AddDays(1), 1m));
        Assert.Throws<DomainException>(() => LogTime(TestData.Today.AddDays(-31), 1m));
        Assert.Throws<DomainException>(() =>
            _service.LogTime(_project, _workPackage, _timeLog, Guid.NewGuid(), TestData.Today, 1m));
    }

    [Fact]
    public void Today_comes_from_the_time_provider()
    {
        _workPackage.StartTask(_task.Id);
        var yesterday = new FakeTimeProvider(new DateTimeOffset(TestData.Today.AddDays(-1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        var service = new TaskTimeLogService(yesterday);

        Assert.Throws<DomainException>(() =>
            service.LogTime(_project, _workPackage, _timeLog, _employeeId, TestData.Today, 1m));
    }

    [Fact]
    public void Wrong_entry_is_removed_only_while_the_task_is_in_progress()
    {
        _workPackage.StartTask(_task.Id);
        var first = LogTime(TestData.Today, 2m);
        var second = LogTime(TestData.Today, 2m);

        _service.RemoveEntry(_workPackage, _timeLog, second.Id);

        Assert.Equal(first.Id, Assert.Single(_timeLog.Entries).Id);
        Assert.Contains(_timeLog.DomainEvents, e => e is TimeEntryRemoved removed && removed.EntryId == second.Id);

        _workPackage.CompleteTask(_task.Id);
        Assert.Throws<DomainException>(() => _service.RemoveEntry(_workPackage, _timeLog, first.Id));
    }

    private TimeEntry LogTime(DateOnly workDate, decimal hours) =>
        _service.LogTime(_project, _workPackage, _timeLog, _employeeId, workDate, hours);
}
