using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagementSystem.Infrastructure;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.ReadModels;

// Applies the migrations of AppDbContext to the database of ConnectionStrings:Default.
// With --script it prints an idempotent SQL script of all migrations instead and does not connect to the database.
// With --rebuild-read-models it also builds the read models anew from the aggregates after migrating, for data saved
// before the projections existed or after a projection changed; stop the API meanwhile, so the outbox does not write.
// Settings: appsettings.json, appsettings.{Environment}.json, user secrets (Development), environment variables,
// command line. It is also the startup project for dotnet ef, which reads the same settings.
var scriptOnly = args.Contains("--script");
var rebuildReadModels = args.Contains("--rebuild-read-models");

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args.Where(arg => arg is not "--script" and not "--rebuild-read-models").ToArray(),
    // appsettings.json is copied to the output, so the migrator finds it whatever the current directory is.
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.AddPersistence(builder.Configuration);

using var host = builder.Build();

await using var scope = host.Services.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

if (scriptOnly)
{
    var script = dbContext.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
    Console.WriteLine(script);
    return 0;
}

try
{
    var commandTimeout = builder.Configuration.GetValue<int?>("Migrator:CommandTimeoutSeconds");
    if (commandTimeout is not null)
        dbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(commandTimeout.Value));

    var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count > 0)
    {
        logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
        await dbContext.Database.MigrateAsync();
    }

    logger.LogInformation("Database is up to date.");

    if (rebuildReadModels)
    {
        logger.LogInformation("Rebuilding the read models.");
        await new ReadModelRebuilder(dbContext).RebuildAsync();
        logger.LogInformation("Read models are rebuilt.");
    }

    return 0;
}
catch (Exception exception)
{
    logger.LogError(exception, "Migration failed.");
    return 1;
}
