using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ConfigureAggregateRoot("Projects");

        builder.Property(p => p.Title).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.State);
        builder.Property(p => p.CreationDate);
        builder.Property(p => p.StartDate);
        builder.Property(p => p.CompletionDate);
        builder.Ignore(p => p.IsOpen);

        builder.ComplexProperty(p => p.Currency, currency =>
            currency.Property(c => c.Code).HasColumnName("CurrencyCode").HasMaxLength(3).IsFixedLength());

        builder.PrimitiveCollection(p => p.WorkPackageIds)
            .HasField("_workPackageIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(p => p.Roles, roles =>
        {
            roles.ToTable("Roles");
            roles.WithOwner().HasForeignKey("ProjectId");

            roles.HasKey(r => r.Id);
            roles.Property(r => r.Id).ValueGeneratedNever();
            roles.Property(r => r.Name).HasMaxLength(100);
            roles.Property(r => r.Description).HasMaxLength(1000);
        });
        builder.Navigation(p => p.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(p => p.TeamMembers, members =>
        {
            members.ToTable("TeamMembers");
            members.WithOwner().HasForeignKey("ProjectId");

            members.HasKey(m => m.Id);
            members.Property(m => m.Id).ValueGeneratedNever();
            members.Property(m => m.EmployeeId);
            members.Property(m => m.RoleId);
            members.Property(m => m.From);
            members.Property(m => m.To);

            // The identity key keeps the rates in the order they were added, which is the order of HourlyRate.From.
            members.OwnsMany(m => m.HourlyRates, rates =>
            {
                rates.ToTable("HourlyRates");
                rates.WithOwner().HasForeignKey("TeamMemberId");

                rates.Property<int>("Id");
                rates.HasKey("Id");
                rates.Property(r => r.Amount).HasPrecision(18, 2);
                rates.Property(r => r.From);
            });
            members.Navigation(m => m.HourlyRates).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Navigation(p => p.TeamMembers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
