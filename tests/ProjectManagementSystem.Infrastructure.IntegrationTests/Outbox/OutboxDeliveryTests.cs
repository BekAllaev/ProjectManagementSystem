using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateRole;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Infrastructure.Persistence.Outbox;

namespace ProjectManagementSystem.Infrastructure.IntegrationTests.Outbox;

/// <summary>
/// Delivery of the outbox with recording handlers of <see cref="ProjectCreated"/> and <see cref="RoleCreated"/>,
/// events that have no handlers in the application.
/// </summary>
public sealed class OutboxDeliveryTests(IntegrationTestDatabase database) : IntegrationTestBase(database)
{
    private readonly DeliveryLog _log = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_log);
        services.AddScoped<IDomainEventHandler<ProjectCreated>, RecordingHandler<ProjectCreated>>();
        services.AddScoped<IDomainEventHandler<RoleCreated>, RecordingHandler<RoleCreated>>();
    }

    [SqlServerFact]
    public async Task Messages_are_delivered_in_the_order_of_saving_and_marked_processed()
    {
        var projectId = (await SendAsync(new CreateProjectCommand("Project", null, "USD"))).Id;
        await SendAsync(new CreateRoleCommand(projectId, "Developer", null));
        await SendAsync(new CreateRoleCommand(projectId, "Tester", null));

        Assert.Null(await DrainOutboxAsync());

        Assert.Equal(
            ["ProjectCreated", "Developer", "Tester"],
            _log.Delivered.Select(e => e is RoleCreated role ? role.Name : e.GetType().Name));
        Assert.All(await AllMessagesAsync(), m => Assert.Equal(Now, m.ProcessedAt));
    }

    [SqlServerFact]
    public async Task A_failed_message_holds_the_queue_until_its_next_attempt()
    {
        _log.FailingType = typeof(RoleCreated);
        _log.FailuresLeft = 1;
        var projectId = (await SendAsync(new CreateProjectCommand("Project", null, "USD"))).Id;
        await SendAsync(new CreateRoleCommand(projectId, "Developer", null));
        await SendAsync(new CreateRoleCommand(projectId, "Tester", null));

        var nextAttemptAt = await DrainOutboxAsync();

        Assert.Equal(Now.AddSeconds(10), nextAttemptAt);
        var messages = await AllMessagesAsync();
        Assert.NotNull(messages[0].ProcessedAt);
        Assert.Equal((1, Now.AddSeconds(10), (DateTime?)null), (messages[1].Attempts, messages[1].NextAttemptAt, messages[1].ProcessedAt));
        Assert.Contains("Handler of RoleCreated failed.", messages[1].LastError);
        Assert.Null(messages[2].ProcessedAt);

        Clock.Advance(TimeSpan.FromSeconds(9));
        Assert.Equal(nextAttemptAt, await DrainOutboxAsync());
        Assert.Equal(1, (await AllMessagesAsync())[1].Attempts);

        Clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Null(await DrainOutboxAsync());

        Assert.All(await AllMessagesAsync(), m => Assert.NotNull(m.ProcessedAt));
        Assert.Equal(
            ["ProjectCreated", "Developer", "Tester"],
            _log.Delivered.Select(e => e is RoleCreated role ? role.Name : e.GetType().Name));
    }

    [SqlServerFact]
    public async Task After_the_last_attempt_the_event_type_is_parked_and_the_rest_of_the_queue_goes_on()
    {
        _log.FailingType = typeof(RoleCreated);
        var projectId = (await SendAsync(new CreateProjectCommand("Project", null, "USD"))).Id;
        await SendAsync(new CreateRoleCommand(projectId, "Developer", null));
        var otherProjectId = (await SendAsync(new CreateProjectCommand("Other", null, "EUR"))).Id;
        await SendAsync(new CreateRoleCommand(projectId, "Tester", null));

        var nextAttemptAt = await DrainOutboxAsync();
        foreach (var backoff in new[] { 10, 60, 600, 3600 })
        {
            Assert.Equal(Now.AddSeconds(backoff), nextAttemptAt);
            Clock.Advance(TimeSpan.FromSeconds(backoff));
            nextAttemptAt = await DrainOutboxAsync();
        }

        Assert.Null(nextAttemptAt);
        var messages = await AllMessagesAsync();
        Assert.Equal(["ProjectCreated", "RoleCreated", "ProjectCreated", "RoleCreated"], messages.Select(m => m.EventType));
        Assert.NotNull(messages[0].ProcessedAt);
        Assert.Equal((5, Now, (DateTime?)null), (messages[1].Attempts, messages[1].ParkedAt, messages[1].ProcessedAt));
        Assert.NotNull(messages[2].ProcessedAt);
        Assert.Equal((0, (DateTime?)null, (DateTime?)null), (messages[3].Attempts, messages[3].ParkedAt, messages[3].ProcessedAt));

        var parked = Assert.Single(await QueryAsync(db => db.ParkedEventTypes.ToListAsync()));
        Assert.Equal(("RoleCreated", Now), (parked.EventType, parked.ParkedAt));
        Assert.Contains(otherProjectId, _log.Delivered.OfType<ProjectCreated>().Select(e => e.ProjectId));
        Assert.Empty(_log.Delivered.OfType<RoleCreated>());
    }

    [SqlServerFact]
    public async Task The_running_processor_delivers_a_message_right_after_the_commit()
    {
        var processor = Services.GetServices<IHostedService>().Single();
        await processor.StartAsync(CancellationToken.None);
        try
        {
            var projectId = (await SendAsync(new CreateProjectCommand("Project", null, "USD"))).Id;

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (!_log.Delivered.OfType<ProjectCreated>().Any(e => e.ProjectId == projectId))
                await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);

            using var processed = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while ((await AllMessagesAsync()).Single().ProcessedAt is null)
                await Task.Delay(TimeSpan.FromMilliseconds(50), processed.Token);
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }
    }

    private Task<List<OutboxMessage>> AllMessagesAsync() =>
        QueryAsync(db => db.OutboxMessages.AsNoTracking().OrderBy(m => m.Sequence).ToListAsync());
}
