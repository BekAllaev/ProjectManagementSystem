using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Infrastructure.Outbox;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.Persistence.Notifications;
using ProjectManagementSystem.Infrastructure.Persistence.Repositories;

namespace ProjectManagementSystem.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    /// <summary>
    /// Registers everything the application needs from the infrastructure: persistence, repositories, the unit of
    /// work, the outbox with its processor, the in-app notifications and the query handlers, which read the database
    /// directly. The processor dispatches events through the application and the handlers run in its pipeline, so the
    /// host also calls <c>AddApplication</c>.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);

        // Only the handlers of this assembly; the pipeline behaviors come from AddApplication.
        services.AddMediatR(mediatR => mediatR.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(EventTypeRegistry.FromAssembly(typeof(IDomainEvent).Assembly));
        services.AddSingleton<DomainEventSerializer>();
        services.AddSingleton<OutboxSignal>();
        services.AddOutboxOptions();
        services.AddSingleton<IOutboxDrainer, OutboxDrainer>();
        services.AddHostedService<OutboxProcessor>();

        services.AddScoped<EfUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<EfUnitOfWork>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<INotificationSender, NotificationSender>();

        return services;
    }

    internal static IServiceCollection AddOutboxOptions(this IServiceCollection services)
    {
        services.AddOptions<OutboxOptions>()
            .Configure<IConfiguration>((options, configuration) => options.Bind(configuration.GetSection(OutboxOptions.SectionName)))
            .Validate(options => options.IsValid(), "Outbox options need non-empty backoff lists and positive limits.");

        return services;
    }

    /// <summary>
    /// Registers only the write-side <see cref="AppDbContext"/> on SQL Server, enough for the migrator. Failed
    /// connections are retried, because a serverless database wakes up with a delay.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure()));

        return services;
    }
}
