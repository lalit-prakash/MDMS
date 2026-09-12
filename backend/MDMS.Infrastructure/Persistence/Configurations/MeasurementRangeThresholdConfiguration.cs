using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class MeasurementRangeThresholdConfiguration : IEntityTypeConfiguration<MeasurementRangeThreshold>
{
    public void Configure(EntityTypeBuilder<MeasurementRangeThreshold> builder)
    {
        builder.ToTable("MeasurementRangeThresholds");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.MinConsumptionKwh).HasColumnType("numeric(18,4)");
        builder.Property(t => t.MaxConsumptionKwh).HasColumnType("numeric(18,4)");
        builder.HasIndex(t => t.MeterId);
    }
}
