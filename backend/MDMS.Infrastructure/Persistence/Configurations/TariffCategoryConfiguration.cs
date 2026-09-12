using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class TariffCategoryConfiguration : IEntityTypeConfiguration<TariffCategory>
{
    public void Configure(EntityTypeBuilder<TariffCategory> builder)
    {
        builder.ToTable("TariffCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(32);
        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Description).HasMaxLength(1024);
    }
}
