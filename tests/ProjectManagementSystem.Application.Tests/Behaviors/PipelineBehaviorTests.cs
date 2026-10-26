using FluentValidation;
using MediatR;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Behaviors;
using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.Tests.Fakes;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Application.Tests.Behaviors;

public class PipelineBehaviorTests
{
    private readonly TestApplication _app = new();

    [Fact]
    public async Task ValidationBehavior_InvalidCommand_RejectsBeforeHandlerAndSavesNothing()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _app.SendAsync(new CreateProjectCommand(Title: "", Description: null, Currency: "EURO")));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(CreateProjectCommand.Title));
        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(CreateProjectCommand.Currency));
        Assert.Empty(_app.Projects.All);
        Assert.Equal(0, _app.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task ValidationBehavior_EmptyIds_RejectsBeforeLoadingAggregates()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _app.SendAsync(new CreateWorkPackageCommand(Guid.Empty, "Design", Guid.Empty)));

        Assert.Equal(2, exception.Errors.Count());
        Assert.Equal(0, _app.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task UnitOfWorkBehavior_SuccessfulCommands_SavesExactlyOncePerCommand()
    {
        await _app.SendAsync(new CreateProjectCommand("First", null, "USD"));
        await _app.SendAsync(new CreateProjectCommand("Second", null, "USD"));

        Assert.Equal(2, _app.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task UnitOfWorkBehavior_HandlerThrows_SavesNothing()
    {
        var project = _app.Projects.Seed(TestData.ProjectWithTeam());
        var notTeamMember = Guid.NewGuid();

        await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            _app.SendAsync(new CreateWorkPackageCommand(project.Id, "Design", notTeamMember)));

        Assert.Equal(0, _app.UnitOfWork.SaveCount);
        Assert.Empty(_app.UnitOfWork.CollectedEvents);
    }

    [Fact]
    public async Task UnitOfWorkBehavior_Query_DoesNotSave()
    {
        var behavior = new UnitOfWorkBehavior<SampleQuery, int>(_app.UnitOfWork);

        var result = await behavior.Handle(new SampleQuery(), _ => Task.FromResult(42), CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(0, _app.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task UnitOfWorkBehavior_Command_SavesAfterHandler()
    {
        var behavior = new UnitOfWorkBehavior<SampleCommand, Unit>(_app.UnitOfWork);
        var saveCountInHandler = -1;

        await behavior.Handle(new SampleCommand(), _ =>
        {
            saveCountInHandler = _app.UnitOfWork.SaveCount;
            return Task.FromResult(Unit.Value);
        }, CancellationToken.None);

        Assert.Equal(0, saveCountInHandler);
        Assert.Equal(1, _app.UnitOfWork.SaveCount);
    }

    private sealed record SampleQuery : IQuery<int>;

    private sealed record SampleCommand : ICommand;
}
