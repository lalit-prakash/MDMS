using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class SavedFilterConfiguration : IEntityTypeConfiguration<SavedFilter>
{
    public void Configure(EntityTypeBuilder<SavedFilter> builder)
    {
        builder.ToTable("SavedFilters");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Screen).IsRequired().HasMaxLength(64);
        builder.Property(f => f.Name).IsRequired().HasMaxLength(100);
        builder.Property(f => f.FilterJson).IsRequired().HasMaxLength(4000);
        builder.HasIndex(f => new { f.UserId, f.Screen, f.Name }).IsUnique();
    }
}
