using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class RiskSignalConfiguration : IEntityTypeConfiguration<RiskSignal>
{
    public void Configure(EntityTypeBuilder<RiskSignal> builder)
    {
        builder.ToTable("RiskSignals");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SignalType).HasConversion<int>();
        builder.Property(s => s.Weight).HasColumnType("numeric(18,4)");
        builder.Property(s => s.Note).HasMaxLength(1024);
        builder.HasIndex(s => s.LeadId);
    }
}
