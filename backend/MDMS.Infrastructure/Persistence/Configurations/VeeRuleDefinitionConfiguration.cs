using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Table-per-hierarchy root for every <see cref="VeeRuleDefinition"/> subclass — one
/// "VeeRuleDefinitions" table, discriminated by <see cref="VeeRuleDefinition.RuleType"/>, so a
/// future config screen can list every configured rule of every type in one query.
/// </summary>
public class VeeRuleDefinitionConfiguration : IEntityTypeConfiguration<VeeRuleDefinition>
{
    public void Configure(EntityTypeBuilder<VeeRuleDefinition> builder)
    {
        builder.ToTable("VeeRuleDefinitions");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.TenantId);
        builder.HasDiscriminator(r => r.RuleType)
            .HasValue<MeasurementRangeThreshold>(VeeRuleType.OutOfRange);
    }
}
