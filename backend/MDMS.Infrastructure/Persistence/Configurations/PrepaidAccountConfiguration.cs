using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class PrepaidAccountConfiguration : IEntityTypeConfiguration<PrepaidAccount>
{
    public void Configure(EntityTypeBuilder<PrepaidAccount> builder)
    {
        builder.ToTable("PrepaidAccounts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Balance).HasColumnType("numeric(18,4)");
        builder.HasIndex(a => a.CustomerId).IsUnique();
    }
}
