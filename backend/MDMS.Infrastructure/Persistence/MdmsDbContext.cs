using MDMS.Application.Common;
using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Infrastructure.Persistence;

public class MdmsDbContext : DbContext, IMdmsDbContext
{
    public MdmsDbContext(DbContextOptions<MdmsDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServicePoint> ServicePoints => Set<ServicePoint>();
    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<MeterAssignment> MeterAssignments => Set<MeterAssignment>();
    public DbSet<LoadSurveyInterval> LoadSurveyIntervals => Set<LoadSurveyInterval>();
    public DbSet<DailyLoadProfile> DailyLoadProfiles => Set<DailyLoadProfile>();
    public DbSet<DataQualityHold> DataQualityHolds => Set<DataQualityHold>();
    public DbSet<VeeRuleDefinition> VeeRuleDefinitions => Set<VeeRuleDefinition>();
    public DbSet<MeasurementRangeThreshold> MeasurementRangeThresholds => Set<MeasurementRangeThreshold>();
    public DbSet<TariffCategory> TariffCategories => Set<TariffCategory>();
    public DbSet<HierarchyNode> HierarchyNodes => Set<HierarchyNode>();
    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();
    public DbSet<User> Users => Set<User>();
    public DbSet<VeeExecutionRecord> VeeExecutionRecords => Set<VeeExecutionRecord>();
    public DbSet<NetworkEnergyReading> NetworkEnergyReadings => Set<NetworkEnergyReading>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<MeterInventoryRecord> MeterInventoryRecords => Set<MeterInventoryRecord>();
    public DbSet<InstallationQualityCheck> InstallationQualityChecks => Set<InstallationQualityCheck>();
    public DbSet<RevenueProtectionLead> RevenueProtectionLeads => Set<RevenueProtectionLead>();
    public DbSet<RiskSignal> RiskSignals => Set<RiskSignal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MdmsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
