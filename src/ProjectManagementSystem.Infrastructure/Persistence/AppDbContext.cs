using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Infrastructure.Identity;
using ProjectManagementSystem.Infrastructure.Persistence.Notifications;
using ProjectManagementSystem.Infrastructure.Persistence.Outbox;

namespace ProjectManagementSystem.Infrastructure.Persistence;

/// <summary>
/// Write side: the aggregates (accessed through <see cref="DbContext.Set{TEntity}()"/> by the repositories), the outbox,
/// the in-app notifications and the accounts of ASP.NET Core Identity; and, in the schema <see cref="ReadSchema"/>, the
/// read models of the read side. Every aggregate is mapped by its own
/// configuration in <c>Persistence/Configurations</c>.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public const string Schema = "dbo";

    /// <summary>
    /// Schema of the read models, kept up to date by the projections in <c>ReadModels/Projections</c>.
    /// </summary>
    public const string ReadSchema = "read";

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ParkedEventType> ParkedEventTypes => Set<ParkedEventType>();

    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity maps its tables first; the configurations below extend them (ApplicationUser).
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
