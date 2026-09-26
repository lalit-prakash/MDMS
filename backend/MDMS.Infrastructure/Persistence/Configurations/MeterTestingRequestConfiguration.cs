using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class MeterTestingRequestConfiguration : IEntityTypeConfiguration<MeterTestingRequest>
{
    public void Configure(EntityTypeBuilder<MeterTestingRequest> builder)
    {
        builder.ToTable("MeterTestingRequests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RequestNumber).IsRequired().HasMaxLength(32);
        builder.HasIndex(r => r.RequestNumber).IsUnique();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.Reason).HasConversion<int>();
        builder.Property(r => r.ConsumerRemarks).HasMaxLength(1000);
        builder.Property(r => r.TestResult).HasMaxLength(500);
        builder.Property(r => r.AccuracyResult).HasMaxLength(500);
        builder.Property(r => r.FinalRemarks).HasMaxLength(1000);
        builder.HasIndex(r => r.CustomerId);
    }
}
