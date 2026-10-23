using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Domain.Employees;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    /// <summary>
    /// Stops two concurrent registrations with the same email that both passed the check of the domain service.
    /// </summary>
    public const string NormalizedEmailIndex = "UX_Employees_NormalizedEmail";

    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ConfigureAggregateRoot("Employees");

        builder.Property(e => e.FirstName).HasMaxLength(100);
        builder.Property(e => e.LastName).HasMaxLength(100);
        builder.Property(e => e.Email).HasMaxLength(254);
        builder.Property(e => e.NormalizedEmail).HasMaxLength(254);
        builder.Property(e => e.PhotoPath).HasMaxLength(500);

        builder.HasIndex(e => e.NormalizedEmail).IsUnique().HasDatabaseName(NormalizedEmailIndex);
    }
}
