using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RequestNumber).IsRequired().HasMaxLength(32);
        builder.HasIndex(r => r.RequestNumber).IsUnique();
        builder.Property(r => r.Category).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.Description).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.ResolutionNote).HasMaxLength(1000);
        builder.HasIndex(r => r.CustomerId);
    }
}
