using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.AccountNumber).IsRequired().HasMaxLength(64);
        builder.HasIndex(c => c.AccountNumber).IsUnique();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(256);
    }
}
