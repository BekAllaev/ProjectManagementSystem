using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectManagementSystem.Infrastructure.Persistence;

/// <summary>
/// For <c>dotnet ef</c>, which has no host project to take the context from. Generating a migration or a script does
/// not connect to the database; <c>dotnet ef database update</c> uses the connection string from the environment
/// variable <c>ConnectionStrings__Default</c>, otherwise the local default instance.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string LocalConnectionString =
        "Server=localhost;Database=ProjectManagementSystem;Trusted_Connection=True;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{DependencyInjection.ConnectionStringName}")
            ?? LocalConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure())
            .Options;

        return new AppDbContext(options);
    }
}
