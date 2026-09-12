using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MDMS.Infrastructure.Persistence.Configurations;

public class DailyLoadProfileConfiguration : IEntityTypeConfiguration<DailyLoadProfile>
{
    public void Configure(EntityTypeBuilder<DailyLoadProfile> builder)
    {
        builder.ToTable("DailyLoadProfiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Source).HasConversion<int>();
        builder.Property(p => p.Quality).HasConversion<int>();
        builder.Property(p => p.ConsumptionKwh).HasColumnType("numeric(18,4)");

        builder.HasIndex(p => new { p.ServicePointId, p.MeterId, p.ProfileDate }).IsUnique();
    }
}
