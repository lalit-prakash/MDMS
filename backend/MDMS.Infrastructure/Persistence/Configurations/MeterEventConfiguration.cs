using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class MeterEventConfiguration : IEntityTypeConfiguration<MeterEvent>
{
    public void Configure(EntityTypeBuilder<MeterEvent> builder)
    {
        builder.ToTable("MeterEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EventType).HasConversion<int>();
        builder.Property(e => e.Severity).HasConversion<int>();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MeterId);
        builder.HasIndex(e => e.OccurredAtUtc);
        builder.HasIndex(e => e.IsAcknowledged);
    }
}
