using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Infrastructure.Outbox;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Tests.Persistence;

/// <summary>
/// The unit of work stamps every event with the version the aggregate is saved with.
/// </summary>
public sealed class EventVersionTests : SqliteTestBase
{
    [Fact]
    public async Task Events_carry_the_version_their_aggregate_is_saved_with()
    {
        var project = TestData.ProjectWithTeam(Me);
        await SaveAsync(projects => projects.Add(project));
        await SaveAsync(async projects =>
        {
            var stored = (await projects.FindAsync(project.Id))!;
            stored.Start(TestData.Now);
            stored.CreateRole("Tester", "");
            projects.Update(stored);
        });

        var events = await OutboxEventsAsync();

        // Created with its role and team member at version 0, then started and given a role in one save at version 1.
        Assert.Equal(
            [("ProjectCreated", 0L), ("RoleCreated", 0L), ("TeamMemberAdded", 0L), ("ProjectStarted", 1L), ("RoleCreated", 1L)],
            events.Select(e => (e.GetType().Name, e.AggregateVersion)));
    }

    private Task SaveAsync(Action<IRepository<Project>> change) =>
        SaveAsync(projects =>
        {
            change(projects);
            return Task.CompletedTask;
        });

    private Task SaveAsync(Func<IRepository<Project>, Task> change) =>
        InScopeAsync(async services =>
        {
            await change(services.GetRequiredService<IRepository<Project>>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

    private async Task<List<IDomainEvent>> OutboxEventsAsync()
    {
        var events = new List<IDomainEvent>();
        await InScopeAsync(async services =>
        {
            var serializer = services.GetRequiredService<DomainEventSerializer>();
            var messages = await services.GetRequiredService<AppDbContext>().OutboxMessages.OrderBy(m => m.Sequence).ToListAsync();
            events.AddRange(messages.Select(m => serializer.Deserialize(m.EventType, m.Payload)));
        });

        return events;
    }
}
