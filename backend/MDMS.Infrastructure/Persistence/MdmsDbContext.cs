using System.Linq.Expressions;
using System.Reflection;
using MDMS.Application.Common;
using MDMS.Domain.Common;
using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Infrastructure.Persistence;

public class MdmsDbContext : DbContext, IMdmsDbContext
{
    private readonly ITenantContext? _tenant;

    /// <param name="tenant">The active organisation. Null (unit tests, design-time tooling) means
    /// no tenant filtering at all; a context with a null <see cref="ITenantContext.TenantId"/>
    /// (unauthenticated request / background job) matches no tenant-scoped rows.</param>
    public MdmsDbContext(DbContextOptions<MdmsDbContext> options, ITenantContext? tenant = null) : base(options)
        => _tenant = tenant;

    private Guid CurrentTenantOrEmpty => _tenant?.TenantId ?? Guid.Empty;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServicePoint> ServicePoints => Set<ServicePoint>();
    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<MeterAssignment> MeterAssignments => Set<MeterAssignment>();
    public DbSet<LoadSurveyInterval> LoadSurveyIntervals => Set<LoadSurveyInterval>();
    public DbSet<DailyLoadProfile> DailyLoadProfiles => Set<DailyLoadProfile>();
    public DbSet<InstantaneousProfile> InstantaneousProfiles => Set<InstantaneousProfile>();
    public DbSet<BillingProfile> BillingProfiles => Set<BillingProfile>();
    public DbSet<MeterEvent> MeterEvents => Set<MeterEvent>();
    public DbSet<DataQualityHold> DataQualityHolds => Set<DataQualityHold>();
    public DbSet<VeeRuleDefinition> VeeRuleDefinitions => Set<VeeRuleDefinition>();
    public DbSet<MeasurementRangeThreshold> MeasurementRangeThresholds => Set<MeasurementRangeThreshold>();
    public DbSet<TariffCategory> TariffCategories => Set<TariffCategory>();
    public DbSet<HierarchyNode> HierarchyNodes => Set<HierarchyNode>();
    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<VeeExecutionRecord> VeeExecutionRecords => Set<VeeExecutionRecord>();
    public DbSet<NetworkEnergyReading> NetworkEnergyReadings => Set<NetworkEnergyReading>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<MeterInventoryRecord> MeterInventoryRecords => Set<MeterInventoryRecord>();
    public DbSet<InstallationQualityCheck> InstallationQualityChecks => Set<InstallationQualityCheck>();
    public DbSet<RevenueProtectionLead> RevenueProtectionLeads => Set<RevenueProtectionLead>();
    public DbSet<RiskSignal> RiskSignals => Set<RiskSignal>();
    public DbSet<PrepaidAccount> PrepaidAccounts => Set<PrepaidAccount>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<SavedFilter> SavedFilters => Set<SavedFilter>();
    public DbSet<DownloadRequest> DownloadRequests => Set<DownloadRequest>();
    public DbSet<UserTenantAccess> UserTenantAccesses => Set<UserTenantAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MdmsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Organisation (tenant) isolation: every tenant-scoped entity is filtered to the active tenant.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clr = entityType.ClrType;
            if (!typeof(Entity).IsAssignableFrom(clr) || typeof(ITenantExempt).IsAssignableFrom(clr) || entityType.BaseType is not null)
                continue;
            typeof(MdmsDbContext)
                .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(clr)
                .Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyTenantFilter<T>(ModelBuilder modelBuilder) where T : Entity
        => modelBuilder.Entity<T>().HasQueryFilter(e => _tenant == null || e.TenantId == CurrentTenantOrEmpty);

    /// <summary>Stamps every newly inserted tenant-scoped row with the active organisation.</summary>
    private void StampTenant()
    {
        if (_tenant?.TenantId is not Guid tenantId) return;
        foreach (var entry in ChangeTracker.Entries<Entity>().Where(e => e.State == EntityState.Added))
            if (entry.Entity is not ITenantExempt)
                entry.Entity.AssignTenant(tenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
