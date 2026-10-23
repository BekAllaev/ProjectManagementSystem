using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ProjectManagementSystem.Application;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Infrastructure.IntegrationTests;

/// <summary>
/// The real <c>AddApplication()</c> and <c>AddInfrastructure()</c> on the test database, emptied before every test.
/// Every save and every load runs in its own scope, so a load always reads the database, never the change tracker.
/// The outbox processor is registered but not started.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase(IntegrationTestDatabase database) : IAsyncLifetime
{
    private ServiceProvider? _services;

    /// <summary>
    /// Start of <see cref="TestData.Today"/>, like the time provider of the domain test data.
    /// </summary>
    protected FakeTimeProvider Clock { get; } = new(new DateTimeOffset(TestData.Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

    protected DateTime Now => Clock.GetUtcNow().UtcDateTime;

    protected static DateOnly Today => TestData.Today;

    protected IServiceProvider Services => _services!;

    public async Task InitializeAsync()
    {
        if (IntegrationTestDatabase.ServerConnectionString is null)
            return;

        await database.ResetAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = database.ConnectionString })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<TimeProvider>(Clock);
        services.AddApplication();
        services.AddInfrastructure(configuration);

        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
            await _services.DisposeAsync();
    }

    /// <summary>
    /// Adds new aggregates through their repositories and saves them in one unit of work.
    /// </summary>
    protected async Task AddAsync<TAggregate>(params TAggregate[] aggregates) where TAggregate : AggregateRoot<Guid>
    {
        await using var scope = Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TAggregate>>();

        foreach (var aggregate in aggregates)
            repository.Add(aggregate);

        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    /// <summary>
    /// Loads the aggregate in a new scope, changes it, updates it through its repository and saves it.
    /// </summary>
    protected async Task UpdateAsync<TAggregate>(Guid id, Action<TAggregate> change) where TAggregate : AggregateRoot<Guid>
    {
        await using var scope = Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TAggregate>>();

        var aggregate = await repository.GetAsync(id);
        change(aggregate);
        repository.Update(aggregate);

        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    /// <summary>
    /// Loads the aggregate in a new scope.
    /// </summary>
    protected async Task<TAggregate> LoadAsync<TAggregate>(Guid id) where TAggregate : AggregateRoot<Guid>
    {
        await using var scope = Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<IRepository<TAggregate>>().GetAsync(id);
    }
}
