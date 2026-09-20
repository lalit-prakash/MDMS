using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class DownloadRequestConfiguration : IEntityTypeConfiguration<DownloadRequest>
{
    public void Configure(EntityTypeBuilder<DownloadRequest> builder)
    {
        builder.ToTable("DownloadRequests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.RequestPath).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.FileName).HasMaxLength(200);
        builder.Property(r => r.ErrorMessage).HasMaxLength(500);
        builder.HasIndex(r => new { r.UserId, r.CreatedAtUtc });
        builder.HasIndex(r => r.Status);
    }
}
