using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).IsRequired().HasMaxLength(128);
        builder.HasIndex(u => new { u.TenantId, u.Username }).IsUnique();
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Role).HasConversion<int>();
        builder.HasIndex(u => u.OrgUnitId);
        builder.Property(u => u.PasswordHash).HasMaxLength(256);
    }
}
