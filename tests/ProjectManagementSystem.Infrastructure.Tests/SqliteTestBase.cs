using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ProjectManagementSystem.Application;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.Outbox;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.Persistence.Outbox;

namespace ProjectManagementSystem.Infrastructure.Tests;

/// <summary>
/// Requests go through the real <c>AddApplication()</c> and <c>AddInfrastructure()</c> (validation, the handlers of the
/// infrastructure) against SQLite in memory: a relational provider, so the LINQ of the handlers is translated to SQL as
/// in production (the EF in-memory provider cannot store the complex property of <c>Project</c>). Aggregates are
/// arranged with domain methods and stored directly. Every request runs in its own scope, like a web request.
/// </summary>
public abstract class SqliteTestBase : IDisposable
{
    private readonly ServiceProvider _services;

    // The in-memory database lives as long as its connection is open.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected SqliteTestBase()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = "Server=localhost;Database=NotUsed" })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<TimeProvider>(Clock);
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddSingleton<ICurrentUser>(CurrentUser);

        foreach (var options in services.Where(d =>
                     d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                     d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>)).ToList())
            services.Remove(options);
        _connection.Open();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        CurrentUser.EmployeeId = Me;

        using var scope = _services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    /// <summary>
    /// Start of <see cref="TestData.Today"/>, the day the team members of <see cref="TestData"/> are counted from.
    /// </summary>
    protected FakeTimeProvider Clock { get; } = new(new DateTimeOffset(TestData.Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

    protected TestCurrentUser CurrentUser { get; } = new();

    /// <summary>
    /// The caller of every request unless a test changes <see cref="CurrentUser"/>.
    /// </summary>
    protected Guid Me { get; } = Guid.NewGuid();

    protected static DateOnly Today => TestData.Today;

    protected static WorkPackageService WorkPackageService { get; } = new(TestData.TimeProvider);

    protected async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    /// <summary>
    /// Stores new aggregates as they are, without the unit of work: their version stays 0.
    /// </summary>
    protected async Task StoreAsync(params AggregateRoot<Guid>[] aggregates)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.AddRange(aggregates);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Stands in for the outbox processor: delivers every saved event to its handlers and projections, in order, and
    /// fails if a delivery failed.
    /// </summary>
    protected async Task DeliverEventsAsync()
    {
        await _services.GetRequiredService<IOutboxDrainer>().DrainAsync(CancellationToken.None);

        await InScopeAsync(async services =>
        {
            var failed = await services.GetRequiredService<AppDbContext>().OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .Select(m => m.EventType + ": " + m.LastError)
                .ToListAsync();
            Assert.True(failed.Count == 0, string.Join(Environment.NewLine, failed));
        });
    }

    /// <summary>
    /// Delivers every event saved so far once more, each in its own scope, as the outbox does after a crash before
    /// marking a message processed.
    /// </summary>
    protected async Task RedeliverAllEventsAsync()
    {
        List<OutboxMessage> messages = [];
        await InScopeAsync(async services =>
            messages = await services.GetRequiredService<AppDbContext>().OutboxMessages.AsNoTracking().OrderBy(m => m.Sequence).ToListAsync());

        foreach (var message in messages)
        {
            await InScopeAsync(async services =>
            {
                var domainEvent = services.GetRequiredService<DomainEventSerializer>().Deserialize(message.EventType, message.Payload);
                await services.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(domainEvent);
                await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            });
        }
    }

    /// <summary>
    /// Every row of a table of the read models, as stored.
    /// </summary>
    protected async Task<List<T>> RowsAsync<T>() where T : class
    {
        List<T> rows = [];
        await InScopeAsync(async services => rows = await services.GetRequiredService<AppDbContext>().Set<T>().AsNoTracking().ToListAsync());

        return rows;
    }

    /// <summary>
    /// Sends a command through the pipeline and delivers the events it saved.
    /// </summary>
    protected async Task<TResponse> SendAndDeliverAsync<TResponse>(IRequest<TResponse> request)
    {
        var response = await SendAsync(request);
        await DeliverEventsAsync();

        return response;
    }

    protected async Task InScopeAsync(Func<IServiceProvider, Task> action)
    {
        await using var scope = _services.CreateAsyncScope();

        await action(scope.ServiceProvider);
    }

    protected static async Task<Employee> EmployeeAsync(string firstName, string lastName) =>
        await new EmployeeService(new InMemoryRepository<Employee>())
            .RegisterAsync(firstName, lastName, $"{firstName}.{lastName}@example.com".ToLowerInvariant());

    protected static ProjectTask AddTask(Project project, WorkPackage workPackage, string name, Guid responsiblePersonId, decimal effort = 8m) =>
        WorkPackageService.AddTask(project, workPackage, name, "Description", responsiblePersonId, effort);

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    protected sealed class TestCurrentUser : ICurrentUser
    {
        public Guid EmployeeId { get; set; }
    }
}
