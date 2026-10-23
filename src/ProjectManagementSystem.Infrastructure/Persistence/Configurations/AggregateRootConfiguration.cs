using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal static class AggregateRootConfiguration
{
    /// <summary>
    /// Table, key, concurrency token and ignored events, the same for every aggregate. Ids are created by the domain.
    /// </summary>
    public static void ConfigureAggregateRoot<TAggregate>(this EntityTypeBuilder<TAggregate> builder, string table)
        where TAggregate : AggregateRoot<Guid>
    {
        builder.ToTable(table);

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        // Raised by the unit of work for every updated aggregate, so a change of an owned collection alone conflicts too.
        builder.Property(a => a.Version).IsConcurrencyToken();

        builder.Ignore(a => a.DomainEvents);
    }
}
