using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class ServicePointConfiguration : IEntityTypeConfiguration<ServicePoint>
{
    public void Configure(EntityTypeBuilder<ServicePoint> builder)
    {
        builder.ToTable("ServicePoints");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Address).IsRequired().HasMaxLength(512);
        builder.HasOne(s => s.Customer).WithMany().HasForeignKey(s => s.CustomerId);
        builder.HasIndex(s => s.DistributionTransformerNodeId);
    }
}
