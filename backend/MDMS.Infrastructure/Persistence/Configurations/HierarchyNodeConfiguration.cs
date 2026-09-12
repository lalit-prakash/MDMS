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
    }
}
