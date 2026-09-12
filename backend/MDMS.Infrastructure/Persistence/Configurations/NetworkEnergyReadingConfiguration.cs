using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class NetworkEnergyReadingConfiguration : IEntityTypeConfiguration<NetworkEnergyReading>
{
    public void Configure(EntityTypeBuilder<NetworkEnergyReading> builder)
    {
        builder.ToTable("NetworkEnergyReadings");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.EnergyKwh).HasColumnType("numeric(18,4)");
        builder.HasIndex(r => new { r.HierarchyNodeId, r.Date }).IsUnique();
    }
}
