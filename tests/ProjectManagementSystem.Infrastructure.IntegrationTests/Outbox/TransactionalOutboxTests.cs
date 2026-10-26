using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Infrastructure.Outbox;

namespace ProjectManagementSystem.Infrastructure.IntegrationTests.Outbox;

public sealed class TransactionalOutboxTests(IntegrationTestDatabase database) : IntegrationTestBase(database)
{
    [SqlServerFact]
    public async Task A_saved_aggregate_and_its_events_are_stored_together()
    {
        var project = TestData.ProjectWithTeam(Guid.NewGuid());

        await AddAsync(project);

        Assert.NotNull(await QueryAsync(db => db.Set<Project>().SingleOrDefaultAsync(p => p.Id == project.Id)));
        var messages = await QueryAsync(db => db.OutboxMessages.OrderBy(m => m.Sequence).ToListAsync());
        Assert.Equal(["ProjectCreated", "RoleCreated", "TeamMemberAdded"], messages.Select(m => m.EventType));
        Assert.All(messages, m =>
        {
            Assert.NotEqual(Guid.Empty, m.EventId);
            Assert.Equal(Now, m.OccurredAt);
            Assert.Null(m.ProcessedAt);
            Assert.Equal(0, m.Attempts);
        });

        var created = Assert.IsType<ProjectCreated>(
            Services.GetRequiredService<DomainEventSerializer>().Deserialize(messages[0].EventType, messages[0].Payload));
        Assert.Equal((project.Id, messages[0].EventId), (created.ProjectId, created.EventId));
    }

    [SqlServerFact]
    public async Task A_failed_save_stores_neither_the_aggregate_nor_its_events()
    {
        Employee ada, twin;
        await using (var scope = Services.CreateAsyncScope())
        {
            var service = new EmployeeService(scope.ServiceProvider.GetRequiredService<IRepository<Employee>>());
            ada = await service.RegisterAsync("Ada", "Lovelace", "ada@example.com");
            twin = await service.RegisterAsync("Ada", "King", "ada@example.com");
        }

        await AddAsync(ada);
        var project = TestData.ProjectWithTeam(Guid.NewGuid());

        // One unit of work: the new project with its events and an employee that violates the unique email.
        await using (var scope = Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IRepository<Project>>().Add(project);
            scope.ServiceProvider.GetRequiredService<IRepository<Employee>>().Add(twin);

            await Assert.ThrowsAsync<DomainRuleViolationException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        }

        Assert.False(await QueryAsync(db => db.Set<Project>().AnyAsync(p => p.Id == project.Id)));
        Assert.False(await QueryAsync(db => db.OutboxMessages.AnyAsync()));
    }
}
