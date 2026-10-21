using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Behaviors;
using ProjectManagementSystem.Application.Events;
using ProjectManagementSystem.Domain.Services;

namespace ProjectManagementSystem.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers command handlers, validators, pipeline behaviors, domain event handlers and domain
    /// services. The infrastructure registers <see cref="IUnitOfWork"/> and the repositories.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);

            // Order matters: the first behavior is the outermost one.
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        AddDomainEventHandlers(services);

        services.AddScoped<ProjectService>();
        services.AddScoped<WorkPackageService>();
        services.AddScoped<ScheduleNetworkService>();
        services.AddScoped<TaskTimeLogService>();
        services.AddScoped<MilestoneService>();
        services.AddScoped<FundingService>();
        services.AddScoped<EmployeeService>();

        return services;
    }

    private static void AddDomainEventHandlers(IServiceCollection services)
    {
        var handlers = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                .Select(serviceType => (serviceType, implementationType: type)));

        foreach (var (serviceType, implementationType) in handlers)
            services.AddScoped(serviceType, implementationType);
    }
}
