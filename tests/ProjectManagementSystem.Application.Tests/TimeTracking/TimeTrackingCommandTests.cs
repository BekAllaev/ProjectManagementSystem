using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;
using ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;
using ProjectManagementSystem.Application.TimeTracking.Commands.RemoveTimeEntry;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.TimeTracking.Events;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Tests.TimeTracking;

public class TimeTrackingCommandTests : CommandTestBase
{
    private readonly Project _project;
    private readonly WorkPackage _workPackage;
    private readonly ProjectTask _task;

    public TimeTrackingCommandTests()
    {
        _project = SeedProject();
        _workPackage = SeedWorkPackage(_project, taskCount: 1);
        _task = _workPackage.Tasks.Single();
        _workPackage.StartTask(_task.Id, _workPackage.ResponsiblePersonId);
        _workPackage.ClearDomainEvents();
    }

    [Fact]
    public async Task LogTime_FirstEntry_CreatesTimeLogWithRateSnapshot()
    {
        var entryId = await App.SendAsync(LogTime(Today, hours: 2m));

        var timeLog = Assert.Single(App.TimeLogs.All);
        Assert.Equal(_task.Id, timeLog.TaskId);
        var entry = Assert.Single(timeLog.Entries);
        Assert.Equal(entryId, entry.Id);
        Assert.Equal(50m, entry.HourlyRate);
        AssertCommitted(timeLog, typeof(TimeLogged));
    }

    [Fact]
    public async Task LogTime_ExistingTimeLog_AddsEntryToIt()
    {
        var timeLog = SeedTimeLogWithEntry(Today, hours: 1m);

        await App.SendAsync(LogTime(Today, hours: 3m));

        Assert.Equal([1m, 3m], timeLog.Entries.Select(e => e.Hours));
        AssertCommitted(timeLog, typeof(TimeLogged));
    }

    [Fact]
    public async Task LogTime_RateChangedLater_EarlierEntryKeepsItsRate()
    {
        await App.SendAsync(LogTime(Today.AddDays(-1), hours: 2m));
        await App.SendAsync(new ChangeHourlyRateCommand(_project.Id, Member, 70m, Today));
        await App.SendAsync(LogTime(Today, hours: 3m));

        var timeLog = Assert.Single(App.TimeLogs.All);
        Assert.Equal([50m, 70m], timeLog.Entries.Select(e => e.HourlyRate));
    }

    [Fact]
    public async Task LogTime_DateAfterTodayOfTimeProvider_ThrowsDomainValidationException()
    {
        await AssertRejectedAsync<DomainValidationException>(() => App.SendAsync(LogTime(Today.AddDays(1), hours: 2m)));
        Assert.Empty(App.TimeLogs.All);
    }

    [Fact]
    public async Task LogTime_TimeProviderAdvanced_AcceptsTheNewToday()
    {
        App.TimeProvider.Advance(TimeSpan.FromDays(1));

        await App.SendAsync(LogTime(Today, hours: 2m));

        Assert.Equal(Today, Assert.Single(App.TimeLogs.All).Entries.Single().WorkDate);
    }

    [Fact]
    public async Task LogTime_TaskNotInProgress_ThrowsDomainRuleViolationException()
    {
        _workPackage.CompleteTask(_task.Id, _workPackage.ResponsiblePersonId);
        _workPackage.ClearDomainEvents();

        await AssertRejectedAsync<DomainRuleViolationException>(() => App.SendAsync(LogTime(Today, hours: 2m)));
        Assert.Empty(App.TimeLogs.All);
    }

    [Fact]
    public async Task LogTime_CallerNotTeamMember_ThrowsDomainForbiddenException()
    {
        App.CurrentUser.EmployeeId = Guid.NewGuid();

        await AssertRejectedAsync<DomainForbiddenException>(() => App.SendAsync(LogTime(Today, hours: 2m)));
    }

    [Fact]
    public async Task LogTime_LogsTimeOfTheCaller()
    {
        await App.SendAsync(LogTime(Today, hours: 2m));

        Assert.Equal(Member, Assert.Single(Assert.Single(App.TimeLogs.All).Entries).EmployeeId);
    }

    [Fact]
    public async Task LogTime_UnknownWorkPackage_ThrowsAggregateNotFoundException()
    {
        await AssertRejectedAsync<AggregateNotFoundException>(() =>
            App.SendAsync(new LogTimeCommand(Guid.NewGuid(), _task.Id, Today, 2m)));
    }

    [Fact]
    public async Task RemoveTimeEntry_TaskInProgress_RemovesEntry()
    {
        var timeLog = SeedTimeLogWithEntry(Today, hours: 1m);

        await App.SendAsync(new RemoveTimeEntryCommand(_task.Id, timeLog.Entries.Single().Id));

        Assert.Empty(timeLog.Entries);
        AssertCommitted(timeLog, typeof(TimeEntryRemoved));
    }

    [Fact]
    public async Task RemoveTimeEntry_TaskCompleted_ThrowsDomainRuleViolationException()
    {
        var timeLog = SeedTimeLogWithEntry(Today, hours: 1m);
        _workPackage.CompleteTask(_task.Id, _workPackage.ResponsiblePersonId);
        _workPackage.ClearDomainEvents();

        await AssertRejectedAsync<DomainRuleViolationException>(() =>
            App.SendAsync(new RemoveTimeEntryCommand(_task.Id, timeLog.Entries.Single().Id)));
        Assert.Single(timeLog.Entries);
    }

    [Fact]
    public async Task RemoveTimeEntry_EntryOfAnotherEmployee_ThrowsDomainForbiddenException()
    {
        var timeLog = SeedTimeLogWithEntry(Today, hours: 1m);
        App.CurrentUser.EmployeeId = Guid.NewGuid();

        await AssertRejectedAsync<DomainForbiddenException>(() =>
            App.SendAsync(new RemoveTimeEntryCommand(_task.Id, timeLog.Entries.Single().Id)));
        Assert.Single(timeLog.Entries);
    }

    private LogTimeCommand LogTime(DateOnly workDate, decimal hours) =>
        new(_workPackage.Id, _task.Id, workDate, hours);

    private TaskTimeLog SeedTimeLogWithEntry(DateOnly workDate, decimal hours)
    {
        var service = new TaskTimeLogService(App.TimeProvider);
        var timeLog = service.CreateTimeLog(_workPackage, _task.Id);
        service.LogTime(_project, _workPackage, timeLog, Member, workDate, hours);

        return App.TimeLogs.Seed(timeLog);
    }
}
