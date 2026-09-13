using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class HierarchyNodeConfiguration : IEntityTypeConfiguration<HierarchyNode>
{
    public void Configure(EntityTypeBuilder<HierarchyNode> builder)
    {
        builder.ToTable("HierarchyNodes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.NodeType).HasConversion<int>();
        builder.Property(n => n.Code).IsRequired().HasMaxLength(32);
        builder.Property(n => n.Name).IsRequired().HasMaxLength(256);
        builder.HasOne(n => n.Parent).WithMany().HasForeignKey(n => n.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(n => new { n.TenantId, n.NodeType, n.Code }).IsUnique();
        builder.Property(n => n.CapacityKva).HasColumnType("numeric(18,4)");
        builder.Property(n => n.VoltageLevel).HasMaxLength(32);
        builder.Property(n => n.Make).HasMaxLength(128);
        builder.Property(n => n.OperationalStatus).HasMaxLength(32);
        builder.Property(n => n.Latitude).HasColumnType("numeric(9,6)");
        builder.Property(n => n.Longitude).HasColumnType("numeric(9,6)");
    }
}
