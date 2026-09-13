using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class BillingProfileConfiguration : IEntityTypeConfiguration<BillingProfile>
{
    public void Configure(EntityTypeBuilder<BillingProfile> builder)
    {
        builder.ToTable("BillingProfiles");
        builder.HasKey(p => p.Id);

        // One BP per meter per billing cycle — the meter creates exactly one at the start of
        // each month, never silently duplicated or overwritten.
        builder.HasIndex(p => new { p.MeterId, p.BillingDate }).IsUnique();

        foreach (var name in new[]
        {
            nameof(BillingProfile.CumulativeKwhImport), nameof(BillingProfile.CumulativeKvahImport),
            nameof(BillingProfile.CumulativeKwhExport), nameof(BillingProfile.CumulativeKvahExport),
            nameof(BillingProfile.AveragePowerFactor),
            nameof(BillingProfile.MaximumDemandKw), nameof(BillingProfile.MaximumDemandKva),
        })
        {
            builder.Property(name).HasColumnType("numeric(18,4)");
        }

        // Native Postgres numeric[] arrays for the 8 tariff-zone splits — simpler and more
        // queryable than 8 separate KwhTz1..KwhTz8 columns for the same data.
        builder.Property(p => p.KwhByTariffZone).HasColumnType("numeric(18,4)[]");
        builder.Property(p => p.KvahByTariffZone).HasColumnType("numeric(18,4)[]");
    }
}
