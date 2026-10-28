using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagementSystem.Application;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Infrastructure;
using ProjectManagementSystem.Infrastructure.DemoData;
using ProjectManagementSystem.Infrastructure.Identity;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.ReadModels;

// Applies the migrations of AppDbContext to the database of ConnectionStrings:Default.
// With --script it prints an idempotent SQL script of all migrations instead and does not connect to the database.
// With --rebuild-read-models it also builds the read models anew from the aggregates after migrating, for data saved
// before the read models existed or after an event handler changed; stop the API meanwhile, so the outbox does not write.
// With --seed it fills an empty database with demo data after migrating; with the setting Seed:Password every demo
// employee also gets a sign-in account (email = the email of the employee). For local and test databases only.
// Settings: appsettings.json, appsettings.{Environment}.json, user secrets (Development), environment variables,
// command line. It is also the startup project for dotnet ef, which reads the same settings.
var scriptOnly = args.Contains("--script");
var rebuildReadModels = args.Contains("--rebuild-read-models");
var seed = args.Contains("--seed");

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args.Where(arg => arg is not "--script" and not "--rebuild-read-models" and not "--seed").ToArray(),
    // appsettings.json is copied to the output, so the migrator finds it whatever the current directory is.
    ContentRootPath = AppContext.BaseDirectory,
});

if (rebuildReadModels || seed)
{
    // The rebuild runs the event handlers and the seeding the commands of the application, which need the whole
    // application and infrastructure. The seeding sends every command on behalf of the employee who may do it.
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddSingleton<MigratorUser>();
    builder.Services.AddSingleton<ICurrentUser>(services => services.GetRequiredService<MigratorUser>());
    builder.Services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<AppDbContext>();
}
else
{
    builder.Services.AddPersistence(builder.Configuration);
}

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
        await scope.ServiceProvider.GetRequiredService<ReadModelRebuilder>().RebuildAsync();
        logger.LogInformation("Read models are rebuilt.");
    }

    if (seed)
    {
        var user = scope.ServiceProvider.GetRequiredService<MigratorUser>();
        var password = builder.Configuration["Seed:Password"];
        var seeder = ActivatorUtilities.CreateInstance<DemoDataSeeder>(scope.ServiceProvider);

        if (await seeder.SeedAsync(employeeId => user.EmployeeId = employeeId, password))
            logger.LogInformation("Demo data seeded{Accounts}.", password is null ? " without accounts (no Seed:Password)" : " with accounts");
        else
            logger.LogWarning("The database already has employees; no demo data seeded.");
    }

    return 0;
}
catch (Exception exception)
{
    logger.LogError(exception, "Migration failed.");
    return 1;
}

/// <summary>
/// The employee the migrator acts for: nobody while it rebuilds, the employee of each command while it seeds.
/// </summary>
internal sealed class MigratorUser : ICurrentUser
{
    public Guid EmployeeId { get; set; }
}
