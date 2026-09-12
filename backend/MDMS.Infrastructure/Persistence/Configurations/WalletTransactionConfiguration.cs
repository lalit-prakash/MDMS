using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Type).HasConversion<int>();
        builder.Property(t => t.Amount).HasColumnType("numeric(18,4)");
        builder.Property(t => t.BalanceAfter).HasColumnType("numeric(18,4)");
        builder.Property(t => t.Reference).IsRequired().HasMaxLength(256);
        builder.HasIndex(t => t.Reference).IsUnique();
        builder.Property(t => t.Note).HasMaxLength(1024);
        builder.HasIndex(t => t.PrepaidAccountId);
    }
}
