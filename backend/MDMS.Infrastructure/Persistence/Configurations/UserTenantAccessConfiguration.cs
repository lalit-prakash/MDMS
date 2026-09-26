using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class UserTenantAccessConfiguration : IEntityTypeConfiguration<UserTenantAccess>
{
    public void Configure(EntityTypeBuilder<UserTenantAccess> builder)
    {
        builder.ToTable("UserTenantAccesses");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.UserId, a.GrantedTenantId }).IsUnique();
    }
}
