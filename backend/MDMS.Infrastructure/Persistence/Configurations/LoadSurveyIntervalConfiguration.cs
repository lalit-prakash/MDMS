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

        // Unique per meter + interval, DB-enforced idempotency — mirrors prepaid_engine.
        builder.HasIndex(i => new { i.MeterId, i.IntervalStartUtc, i.IntervalEndUtc }).IsUnique();
    }
}
