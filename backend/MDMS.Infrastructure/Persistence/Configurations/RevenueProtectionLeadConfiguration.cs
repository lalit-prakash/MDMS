using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class RevenueProtectionLeadConfiguration : IEntityTypeConfiguration<RevenueProtectionLead>
{
    public void Configure(EntityTypeBuilder<RevenueProtectionLead> builder)
    {
        builder.ToTable("RevenueProtectionLeads");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Status).HasConversion<int>();
        builder.Property(l => l.RiskScore).HasColumnType("numeric(18,4)");
        builder.Property(l => l.RecoveryAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.FieldFindingNote).HasMaxLength(2000);
        builder.Property(l => l.ActionTaken).HasMaxLength(2000);
        builder.Property(l => l.ClosureReason).HasMaxLength(2000);
        builder.HasIndex(l => l.CustomerId);
        builder.HasIndex(l => l.Status);
    }
}
