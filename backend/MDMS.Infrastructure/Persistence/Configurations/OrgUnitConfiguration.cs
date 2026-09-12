using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class OrgUnitConfiguration : IEntityTypeConfiguration<OrgUnit>
{
    public void Configure(EntityTypeBuilder<OrgUnit> builder)
    {
        builder.ToTable("OrgUnits");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UnitType).HasConversion<int>();
        builder.Property(u => u.Code).IsRequired().HasMaxLength(32);
        builder.Property(u => u.Name).IsRequired().HasMaxLength(256);
        builder.HasOne(u => u.Parent).WithMany().HasForeignKey(u => u.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(u => new { u.TenantId, u.UnitType, u.Code }).IsUnique();
    }
}
