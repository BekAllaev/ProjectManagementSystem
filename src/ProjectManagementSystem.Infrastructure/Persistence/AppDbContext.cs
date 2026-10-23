using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Infrastructure.Persistence.Notifications;
using ProjectManagementSystem.Infrastructure.Persistence.Outbox;

namespace ProjectManagementSystem.Infrastructure.Persistence;

/// <summary>
/// Write side: the aggregates (accessed through <see cref="DbContext.Set{TEntity}()"/> by the repositories), the outbox
/// and the in-app notifications. Every aggregate is mapped by its own configuration in <c>Persistence/Configurations</c>.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public const string Schema = "dbo";

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ParkedEventType> ParkedEventTypes => Set<ParkedEventType>();

    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
