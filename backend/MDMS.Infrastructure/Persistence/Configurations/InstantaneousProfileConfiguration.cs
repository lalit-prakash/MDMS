using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class InstantaneousProfileConfiguration : IEntityTypeConfiguration<InstantaneousProfile>
{
    public void Configure(EntityTypeBuilder<InstantaneousProfile> builder)
    {
        builder.ToTable("InstantaneousProfiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.LoadLimitState).HasConversion<int>();

        // One IP reading per meter per 15-minute timestamp — never silently duplicated.
        builder.HasIndex(p => new { p.MeterId, p.MeterTimeUtc }).IsUnique();
        builder.HasIndex(p => p.MeterTimeUtc);

        foreach (var name in new[]
        {
            nameof(InstantaneousProfile.Voltage), nameof(InstantaneousProfile.PhaseCurrent), nameof(InstantaneousProfile.NeutralCurrent),
            nameof(InstantaneousProfile.PowerFactor), nameof(InstantaneousProfile.Frequency),
            nameof(InstantaneousProfile.Kw), nameof(InstantaneousProfile.Kva), nameof(InstantaneousProfile.Kwh), nameof(InstantaneousProfile.Kvah),
            nameof(InstantaneousProfile.KwhExport), nameof(InstantaneousProfile.KvahExport),
            nameof(InstantaneousProfile.MdKw), nameof(InstantaneousProfile.MdKva),
            nameof(InstantaneousProfile.MdKwExport), nameof(InstantaneousProfile.MdKvaExport),
            nameof(InstantaneousProfile.LoadLimitValue),
        })
        {
            builder.Property(name).HasColumnType("numeric(18,4)");
        }
    }
}
