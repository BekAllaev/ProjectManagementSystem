using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjectManagementSystem.Infrastructure.Persistence;
using Respawn;

namespace ProjectManagementSystem.Infrastructure.IntegrationTests;

/// <summary>
/// A database of its own for one test run on the local SQL Server: created with a unique name and migrated at start,
/// dropped at the end. Tests empty it with <see cref="ResetAsync"/>. The server comes from the setting
/// <c>IntegrationTests:SqlServer</c> (user secrets or the environment variable <c>IntegrationTests__SqlServer</c>),
/// never from the repository; without it every test is skipped.
/// </summary>
public sealed class IntegrationTestDatabase : IAsyncLifetime
{
    public const string SettingName = "IntegrationTests:SqlServer";

    private Respawner? _respawner;

    public IntegrationTestDatabase()
    {
        if (ServerConnectionString is null)
            return;

        ConnectionString = new SqlConnectionStringBuilder(ServerConnectionString)
        {
            InitialCatalog = $"ProjectManagementSystem_IntegrationTests_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    /// <summary>
    /// Connection string of the server, or <see langword="null"/> when the integration tests are not configured.
    /// </summary>
    public static string? ServerConnectionString { get; } = new ConfigurationBuilder()
        .AddUserSecrets<IntegrationTestDatabase>(optional: true)
        .AddEnvironmentVariables()
        .Build()[SettingName] is { Length: > 0 } value ? value : null;

    public string ConnectionString { get; } = string.Empty;

    public async Task InitializeAsync()
    {
        if (ServerConnectionString is null)
            return;

        await using (var dbContext = CreateDbContext())
            await dbContext.Database.MigrateAsync();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = ["__EFMigrationsHistory"],
        });
    }

    /// <summary>
    /// Deletes all data, keeps the schema.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    public async Task DisposeAsync()
    {
        if (ServerConnectionString is null)
            return;

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);
}

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestDatabase>
{
    public const string Name = "SQL Server";
}

/// <summary>
/// A test that needs the SQL Server; skipped when <see cref="IntegrationTestDatabase.SettingName"/> is not set.
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (IntegrationTestDatabase.ServerConnectionString is null)
            Skip = $"Integration tests need the setting '{IntegrationTestDatabase.SettingName}' (user secrets or environment).";
    }
}
