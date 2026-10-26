using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Infrastructure.Identity;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// Adds the employee to the account table of ASP.NET Core Identity, which configures the rest.
/// </summary>
internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    /// <summary>
    /// One employee has at most one account.
    /// </summary>
    public const string EmployeeIdIndex = "UX_AspNetUsers_EmployeeId";

    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.EmployeeId).IsRequired();
        builder.HasIndex(u => u.EmployeeId).IsUnique().HasDatabaseName(EmployeeIdIndex);

        // A foreign key without navigations: the account cannot point to a missing employee, the domain stays unaware.
        builder.HasOne<Employee>()
            .WithOne()
            .HasForeignKey<ApplicationUser>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
