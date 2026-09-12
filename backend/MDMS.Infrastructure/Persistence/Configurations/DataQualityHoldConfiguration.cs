using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class DataQualityHoldConfiguration : IEntityTypeConfiguration<DataQualityHold>
{
    public void Configure(EntityTypeBuilder<DataQualityHold> builder)
    {
        builder.ToTable("DataQualityHolds");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Reason).IsRequired().HasMaxLength(1024);
        builder.Property(h => h.ResolutionNote).HasMaxLength(1024);
        builder.HasIndex(h => new { h.MeterId, h.IsActive });
    }
}
