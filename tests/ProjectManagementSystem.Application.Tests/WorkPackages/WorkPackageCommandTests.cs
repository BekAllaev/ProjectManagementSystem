using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.ChangeTaskResponsible;
using ProjectManagementSystem.Application.WorkPackages.Commands.ChangeWorkPackageResponsible;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Application.Tests.WorkPackages;

public class WorkPackageCommandTests : CommandTestBase
{
    private readonly Guid _colleague = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    [Fact]
    public async Task ChangeWorkPackageResponsible_TeamMember_ChangesOnlyWorkPackage()
    {
        var project = SeedProject(_colleague);
        var workPackage = SeedWorkPackage(project);

        await App.SendAsync(new ChangeWorkPackageResponsibleCommand(workPackage.Id, _colleague));

        Assert.Equal(_colleague, workPackage.ResponsiblePersonId);
        AssertCommitted(workPackage, typeof(ResponsiblePersonChanged));
    }

    [Fact]
    public async Task ChangeWorkPackageResponsible_NotTeamMember_ThrowsDomainRuleViolationException()
    {
        var workPackage = SeedWorkPackage(SeedProject());

        await AssertRejectedAsync<DomainRuleViolationException>(() =>
            App.SendAsync(new ChangeWorkPackageResponsibleCommand(workPackage.Id, _outsider)));
        Assert.Equal(Member, workPackage.ResponsiblePersonId);
    }

    [Fact]
    public async Task CompleteWorkPackage_AllTasksFinished_CompletesIt()
    {
        var workPackage = SeedWorkPackage(SeedProject(), taskCount: 1);
        workPackage.CancelTask(workPackage.Tasks.Single().Id);
        workPackage.ClearDomainEvents();

        await App.SendAsync(new CompleteWorkPackageCommand(workPackage.Id));

        Assert.Equal(WorkPackageStatus.Completed, workPackage.Status);
        AssertCommitted(workPackage, typeof(WorkPackageCompleted));
    }

    [Fact]
    public async Task CompleteWorkPackage_UnfinishedTask_ThrowsDomainRuleViolationException()
    {
        var workPackage = SeedWorkPackage(SeedProject(), taskCount: 1);

        await AssertRejectedAsync<DomainRuleViolationException>(() => App.SendAsync(new CompleteWorkPackageCommand(workPackage.Id)));
    }

    [Fact]
    public async Task CancelWorkPackage_OpenWorkPackage_CancelsItAndItsUnfinishedTasks()
    {
        var workPackage = SeedWorkPackage(SeedProject(), taskCount: 2);
        var completedTask = workPackage.Tasks.First();
        workPackage.StartTask(completedTask.Id);
        workPackage.CompleteTask(completedTask.Id);
        workPackage.ClearDomainEvents();

        await App.SendAsync(new CancelWorkPackageCommand(workPackage.Id, "Out of scope"));

        Assert.Equal(WorkPackageStatus.Cancelled, workPackage.Status);
        Assert.Equal("Out of scope", workPackage.CancellationReason);
        Assert.Equal([ProjectTaskStatus.Completed, ProjectTaskStatus.Cancelled], workPackage.Tasks.Select(t => t.Status));
        AssertCommitted(workPackage, typeof(WorkPackageCancelled));
        var cancelled = Assert.IsType<WorkPackageCancelled>(App.UnitOfWork.CollectedEvents.Single());
        Assert.Equal([workPackage.Tasks.Last().Id], cancelled.CancelledTaskIds);
    }

    [Fact]
    public async Task CancelWorkPackage_AlreadyCancelled_ThrowsDomainRuleViolationException()
    {
        var workPackage = SeedWorkPackage(SeedProject());
        workPackage.Cancel("First");
        workPackage.ClearDomainEvents();

        await AssertRejectedAsync<DomainRuleViolationException>(() => App.SendAsync(new CancelWorkPackageCommand(workPackage.Id, "Second")));
    }

    [Fact]
    public async Task CreateTask_TeamMemberResponsible_AddsOpenTaskAndChangesOnlyWorkPackage()
    {
        var project = SeedProject(_colleague);
        var workPackage = SeedWorkPackage(project);

        var taskId = await App.SendAsync(new CreateTaskCommand(workPackage.Id, "Mockups", null, _colleague, EstimatedEffort: 8m));

        var task = workPackage.GetTask(taskId);
        Assert.Equal(ProjectTaskStatus.Open, task.Status);
        Assert.Equal(_colleague, task.ResponsiblePersonId);
        Assert.Equal(8m, task.EstimatedEffort);
        AssertCommitted(workPackage, typeof(TaskAdded));
    }

    [Fact]
    public async Task CreateTask_NotTeamMember_ThrowsDomainRuleViolationException()
    {
        var workPackage = SeedWorkPackage(SeedProject());

        await AssertRejectedAsync<DomainRuleViolationException>(() =>
            App.SendAsync(new CreateTaskCommand(workPackage.Id, "Mockups", null, _outsider, 8m)));
        Assert.Empty(workPackage.Tasks);
    }

    [Fact]
    public async Task CreateTask_CancelledWorkPackage_ThrowsDomainRuleViolationException()
    {
        var workPackage = SeedWorkPackage(SeedProject());
        workPackage.Cancel("Out of scope");
        workPackage.ClearDomainEvents();

        await AssertRejectedAsync<DomainRuleViolationException>(() =>
            App.SendAsync(new CreateTaskCommand(workPackage.Id, "Mockups", null, Member, 8m)));
    }

    [Fact]
    public async Task CreateTask_UnknownWorkPackage_ThrowsAggregateNotFoundException()
    {
        await AssertRejectedAsync<AggregateNotFoundException>(() =>
            App.SendAsync(new CreateTaskCommand(Guid.NewGuid(), "Mockups", null, Member, 8m)));
    }

    [Fact]
    public async Task ChangeTaskResponsible_TeamMember_ChangesIt()
    {
        var workPackage = SeedWorkPackage(SeedProject(_colleague), taskCount: 1);
        var task = workPackage.Tasks.Single();

        await App.SendAsync(new ChangeTaskResponsibleCommand(workPackage.Id, task.Id, _colleague));

        Assert.Equal(_colleague, task.ResponsiblePersonId);
        AssertCommitted(workPackage, typeof(ResponsiblePersonChanged));
    }

    [Fact]
    public async Task ChangeTaskResponsible_NotTeamMember_ThrowsDomainRuleViolationException()
    {
        var workPackage = SeedWorkPackage(SeedProject(), taskCount: 1);

        await AssertRejectedAsync<DomainRuleViolationException>(() =>
            App.SendAsync(new ChangeTaskResponsibleCommand(workPackage.Id, workPackage.Tasks.Single().Id, _outsider)));
    }

    [Fact]
    public async Task StartTask_OpenTask_StartsIt()
    {
        var workPackage = SeedWorkPackage(SeedProject(), taskCount: 1);
        var task = workPackage.Tasks.Single();

        await App.SendAsync(new StartTaskCommand(workPackage.Id, task.Id));

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        AssertCommitted(workPackage, typeof(TaskStarted));
    }

    [Fact]
    public async Task StartTask_UnknownTask_ThrowsEntityNotFoundException()
    {
        var workPackage = SeedWorkPackage(SeedProject());

        await AssertRejectedAsync<EntityNotFoundException>(() => App.SendAsync(new StartTaskCommand(workPackage.Id, Guid.NewGuid())));
    }

    [Fact]
    public async Task CompleteTask_TaskInProgress_CompletesIt()
    {
        var workPackage = SeedWorkPackage(SeedProject(), taskCount: 1);
        var task = workPackage.Tasks.Single();
        workPackage.StartTask(task.Id);
        workPackage.ClearDomainEvents();

        await App.SendAsync(new CompleteTaskCommand(workPackage.Id, task.Id));

        Assert.Equal(ProjectTaskStatus.Completed, task.Status);
        AssertCommitted(workPackage, typeof(TaskCompleted));
    }

    [Fact]
    public async Task CompleteTask_OpenTask_ThrowsDomainRuleViolationException()
    {
        var workPackage = SeedWorkPackage(SeedProject(), taskCount: 1);

        await AssertRejectedAsync<DomainRuleViolationException>(() =>
            App.SendAsync(new CompleteTaskCommand(workPackage.Id, workPackage.Tasks.Single().Id)));
    }

    [Fact]
    public async Task CancelTask_OpenTask_CancelsIt()
    {
        var workPackage = SeedWorkPackage(SeedProject(), taskCount: 1);
        var task = workPackage.Tasks.Single();

        await App.SendAsync(new CancelTaskCommand(workPackage.Id, task.Id));

        Assert.Equal(ProjectTaskStatus.Cancelled, task.Status);
        AssertCommitted(workPackage, typeof(TaskCancelled));
    }

    [Fact]
    public async Task CancelTask_CompletedTask_ThrowsDomainRuleViolationException()
    {
        var workPackage = SeedWorkPackage(SeedProject(), taskCount: 1);
        var task = workPackage.Tasks.Single();
        workPackage.StartTask(task.Id);
        workPackage.CompleteTask(task.Id);
        workPackage.ClearDomainEvents();

        await AssertRejectedAsync<DomainRuleViolationException>(() => App.SendAsync(new CancelTaskCommand(workPackage.Id, task.Id)));
    }
}
