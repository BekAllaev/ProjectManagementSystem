using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application;
using ProjectManagementSystem.Application.Abstractions;

namespace ProjectManagementSystem.Infrastructure.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void Application_and_infrastructure_together_resolve_every_registration()
    {
        // Building the container does not connect to the database.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=localhost;Database=NotUsed;Trusted_Connection=True",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication();
        services.AddInfrastructure(configuration);
        // Registered by the host from the authenticated caller.
        services.AddScoped<ICurrentUser, AnonymousCurrentUser>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.NotNull(provider);
    }

    private sealed class AnonymousCurrentUser : ICurrentUser
    {
        public Guid EmployeeId => Guid.Empty;
    }
}
