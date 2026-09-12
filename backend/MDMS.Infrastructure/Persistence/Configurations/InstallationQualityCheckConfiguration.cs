using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class InstallationQualityCheckConfiguration : IEntityTypeConfiguration<InstallationQualityCheck>
{
    public void Configure(EntityTypeBuilder<InstallationQualityCheck> builder)
    {
        builder.ToTable("InstallationQualityChecks");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Status).HasConversion<int>();
        builder.Property(c => c.L2RejectionNote).HasMaxLength(1024);
        builder.Property(c => c.L3RejectionNote).HasMaxLength(1024);
        builder.HasIndex(c => c.MeterId);
    }
}
