using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Infrastructure.IntegrationTests.Persistence;

public sealed class ConcurrencyTests(IntegrationTestDatabase database) : IntegrationTestBase(database)
{
    [SqlServerFact]
    public async Task Two_changes_of_owned_collections_only_conflict_on_the_version()
    {
        var project = TestData.ProjectWithTeam(Guid.NewGuid());
        await AddAsync(project);

        await using var first = Services.CreateAsyncScope();
        await using var second = Services.CreateAsyncScope();
        var firstProject = await first.ServiceProvider.GetRequiredService<IRepository<Project>>().GetAsync(project.Id);
        var secondProject = await second.ServiceProvider.GetRequiredService<IRepository<Project>>().GetAsync(project.Id);

        firstProject.CreateRole("Tester", "");
        first.ServiceProvider.GetRequiredService<IRepository<Project>>().Update(firstProject);
        secondProject.CreateRole("Manager", "");
        second.ServiceProvider.GetRequiredService<IRepository<Project>>().Update(secondProject);

        await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        var conflict = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());

        Assert.Equal((nameof(Project), project.Id), (conflict.AggregateName, conflict.Id));
        var stored = await LoadAsync<Project>(project.Id);
        Assert.Equal(1, stored.Version);
        Assert.Contains(stored.Roles, r => r.Name == "Tester");
        Assert.DoesNotContain(stored.Roles, r => r.Name == "Manager");
        Assert.Equal(1, await QueryAsync(db => db.OutboxMessages.CountAsync(m => m.EventType == "RoleCreated" && m.Payload.Contains("Tester"))));
        Assert.Equal(0, await QueryAsync(db => db.OutboxMessages.CountAsync(m => m.Payload.Contains("Manager"))));
    }

    [SqlServerFact]
    public async Task Of_two_concurrent_registrations_with_the_same_email_one_fails_with_the_domain_error()
    {
        await using var first = Services.CreateAsyncScope();
        await using var second = Services.CreateAsyncScope();
        var firstRepository = first.ServiceProvider.GetRequiredService<IRepository<Employee>>();
        var secondRepository = second.ServiceProvider.GetRequiredService<IRepository<Employee>>();

        // Both pass the check of the domain service before either is saved.
        var ada = await new EmployeeService(firstRepository).RegisterAsync("Ada", "Lovelace", "ada@example.com");
        var twin = await new EmployeeService(secondRepository).RegisterAsync("Ada", "King", "ADA@example.com");

        firstRepository.Add(ada);
        await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        secondRepository.Add(twin);
        var error = await Assert.ThrowsAsync<DomainException>(
            () => second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());

        Assert.Equal(EmployeeService.EmailIsTaken("ada@example.com").Message, error.Message);
        Assert.Equal([ada.Id], await QueryAsync(db => db.Set<Employee>().Select(e => e.Id).ToListAsync()));
    }
}
