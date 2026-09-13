using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class LoadSurveyIntervalConfiguration : IEntityTypeConfiguration<LoadSurveyInterval>
{
    public void Configure(EntityTypeBuilder<LoadSurveyInterval> builder)
    {
        builder.ToTable("LoadSurveyIntervals");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Quality).HasConversion<int>();
        builder.Property(i => i.Source).HasConversion<int>();
        builder.Property(i => i.CumulativeReading).HasColumnType("numeric(18,4)");
        builder.Property(i => i.ConsumptionKwh).HasColumnType("numeric(18,4)");
        foreach (var name in new[] { nameof(LoadSurveyInterval.AverageVoltage), nameof(LoadSurveyInterval.AverageCurrent), nameof(LoadSurveyInterval.CumulativeKvahImport), nameof(LoadSurveyInterval.CumulativeKwhExport), nameof(LoadSurveyInterval.CumulativeKvahExport) })
            builder.Property(name).HasColumnType("numeric(18,4)");

        // Unique per meter + interval — DB-enforced idempotency, a second layer atop the
        // ingestion service's own in-batch duplicate check.
        builder.HasIndex(i => new { i.MeterId, i.IntervalStartUtc, i.IntervalEndUtc }).IsUnique();
    }
}
