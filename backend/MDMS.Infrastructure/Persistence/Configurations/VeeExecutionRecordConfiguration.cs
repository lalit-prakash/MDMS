using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class VeeExecutionRecordConfiguration : IEntityTypeConfiguration<VeeExecutionRecord>
{
    public void Configure(EntityTypeBuilder<VeeExecutionRecord> builder)
    {
        builder.ToTable("VeeExecutionRecords");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RuleName).IsRequired().HasMaxLength(128);
        builder.Property(r => r.MeasurementType).HasConversion<int>();
        builder.Property(r => r.ResultQuality).HasConversion<int>();
        builder.Property(r => r.NewValue).HasColumnType("numeric(18,4)");
        builder.Property(r => r.Details).IsRequired().HasMaxLength(1024);
        builder.HasIndex(r => new { r.MeterId, r.SlotStartUtc });
    }
}
