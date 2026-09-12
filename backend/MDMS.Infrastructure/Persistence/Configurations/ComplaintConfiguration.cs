using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("Complaints");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Source).HasConversion<int>();
        builder.Property(c => c.Description).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.Status).HasConversion<int>();
        builder.Property(c => c.ResolutionNote).HasMaxLength(2000);
        builder.HasIndex(c => c.CustomerId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.SlaDueUtc);
    }
}
