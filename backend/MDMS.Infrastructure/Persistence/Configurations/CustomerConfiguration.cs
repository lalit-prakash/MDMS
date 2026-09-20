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
        builder.Property(c => c.RrNumber).HasMaxLength(64);
        builder.Property(c => c.MobileNumber).HasMaxLength(32);
        builder.Property(c => c.ConnectionStatus).HasMaxLength(32);
        builder.Property(c => c.LoadType).HasMaxLength(32);
        builder.Property(c => c.TariffCategoryCode).HasMaxLength(32);
        builder.Property(c => c.CommunicationType).HasMaxLength(32);
        builder.Property(c => c.PaymentMode).HasMaxLength(32);
        builder.Property(c => c.BillCycle).HasMaxLength(32);
        builder.Property(c => c.SanctionedLoadKw).HasColumnType("numeric(18,4)");
        builder.Property(c => c.ContractDemandKva).HasColumnType("numeric(18,4)");
        builder.Property(c => c.ConnectedLoadKw).HasColumnType("numeric(18,4)");
        builder.Property(c => c.Latitude).HasColumnType("numeric(9,6)");
        builder.Property(c => c.Longitude).HasColumnType("numeric(9,6)");
        builder.Property(c => c.MeterMake).HasMaxLength(64);
        builder.Property(c => c.MeterPhase).HasMaxLength(16);
        builder.Property(c => c.MultiplyingFactor).HasColumnType("numeric(18,4)");
    }
}
