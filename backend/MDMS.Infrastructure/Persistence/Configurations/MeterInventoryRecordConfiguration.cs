using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class MeterInventoryRecordConfiguration : IEntityTypeConfiguration<MeterInventoryRecord>
{
    public void Configure(EntityTypeBuilder<MeterInventoryRecord> builder)
    {
        builder.ToTable("MeterInventoryRecords");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.ReplacementReason).HasMaxLength(1024);
        builder.HasIndex(r => r.MeterId).IsUnique();
    }
}
