using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

/// <summary>Configures only the columns specific to this <see cref="VeeRuleDefinition"/> subclass — see <see cref="VeeRuleDefinitionConfiguration"/> for the shared table/key/discriminator setup.</summary>
public class MeasurementRangeThresholdConfiguration : IEntityTypeConfiguration<MeasurementRangeThreshold>
{
    public void Configure(EntityTypeBuilder<MeasurementRangeThreshold> builder)
    {
        builder.Property(t => t.MeasurementType).HasConversion<int>();
        builder.Property(t => t.MinConsumptionKwh).HasColumnType("numeric(18,4)");
        builder.Property(t => t.MaxConsumptionKwh).HasColumnType("numeric(18,4)");
        builder.HasIndex(t => new { t.MeasurementType, t.MeterId });
    }
}
