using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Domain.Funding;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class ProjectFundingConfiguration : IEntityTypeConfiguration<ProjectFunding>
{
    public void Configure(EntityTypeBuilder<ProjectFunding> builder)
    {
        builder.ConfigureAggregateRoot("ProjectFundings");

        // One funding per project: its id is the id of the project.
        builder.Property(f => f.Id).HasColumnName("ProjectId");
        builder.Ignore(f => f.ProjectId);

        // Tranches have no identity of their own; the identity key keeps them in the order they were added.
        builder.OwnsMany(f => f.Tranches, tranches =>
        {
            tranches.ToTable("FundingTranches");
            tranches.WithOwner().HasForeignKey("ProjectId");

            tranches.Property<int>("Id");
            tranches.HasKey("Id");
            tranches.Property(t => t.Amount).HasPrecision(18, 2);
            tranches.Property(t => t.Date);
            tranches.Property(t => t.Reason).HasMaxLength(1000);
        });
        builder.Navigation(f => f.Tranches).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
