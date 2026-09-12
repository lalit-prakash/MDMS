using MDMS.Application.Common;
using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Infrastructure.Persistence;

public class MdmsDbContext : DbContext, IMdmsDbContext
{
    public MdmsDbContext(DbContextOptions<MdmsDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServicePoint> ServicePoints => Set<ServicePoint>();
    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<MeterAssignment> MeterAssignments => Set<MeterAssignment>();
    public DbSet<LoadSurveyInterval> LoadSurveyIntervals => Set<LoadSurveyInterval>();
    public DbSet<DailyLoadProfile> DailyLoadProfiles => Set<DailyLoadProfile>();
    public DbSet<DataQualityHold> DataQualityHolds => Set<DataQualityHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MdmsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
