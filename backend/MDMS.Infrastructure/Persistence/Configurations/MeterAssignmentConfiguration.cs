using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class MeterAssignmentConfiguration : IEntityTypeConfiguration<MeterAssignment>
{
    public void Configure(EntityTypeBuilder<MeterAssignment> builder)
    {
        builder.ToTable("MeterAssignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EventType).HasConversion<int>();
        builder.Property(a => a.Reason).HasMaxLength(512);
        builder.HasOne(a => a.Meter).WithMany().HasForeignKey(a => a.MeterId);
        builder.HasIndex(a => a.ServicePointId);
        builder.HasIndex(a => a.MeterId);
    }
}
