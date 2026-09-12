using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class MeterConfiguration : IEntityTypeConfiguration<Meter>
{
    public void Configure(EntityTypeBuilder<Meter> builder)
    {
        builder.ToTable("Meters");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.SerialNumber).IsRequired().HasMaxLength(64);
        builder.HasIndex(m => m.SerialNumber).IsUnique();
        builder.Property(m => m.Phase).HasConversion<int>();
        builder.Property(m => m.Status).HasConversion<int>();
    }
}
