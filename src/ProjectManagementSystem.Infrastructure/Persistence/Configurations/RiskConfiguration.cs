using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class RiskConfiguration : IEntityTypeConfiguration<Risk>
{
    public void Configure(EntityTypeBuilder<Risk> builder)
    {
        builder.ConfigureAggregateRoot("Risks");

        builder.Property(r => r.ProjectId);
        builder.Property(r => r.Description).HasMaxLength(2000);
        builder.HasIndex(r => r.ProjectId);
    }
}
