using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Application.Common;

/// <summary>
/// The persistence seam the Application layer depends on, implemented by Infrastructure's
/// EF Core <c>MdmsDbContext</c>. Keeps Application free of a direct EF Core/Npgsql dependency.
/// </summary>
public interface IMdmsDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Customer> Customers { get; }
    DbSet<ServicePoint> ServicePoints { get; }
    DbSet<Meter> Meters { get; }
    DbSet<MeterAssignment> MeterAssignments { get; }
    DbSet<LoadSurveyInterval> LoadSurveyIntervals { get; }
    DbSet<DailyLoadProfile> DailyLoadProfiles { get; }
    DbSet<InstantaneousProfile> InstantaneousProfiles { get; }
    DbSet<BillingProfile> BillingProfiles { get; }
    DbSet<MeterEvent> MeterEvents { get; }
    DbSet<DataQualityHold> DataQualityHolds { get; }
    DbSet<VeeRuleDefinition> VeeRuleDefinitions { get; }
    DbSet<MeasurementRangeThreshold> MeasurementRangeThresholds { get; }
    DbSet<TariffCategory> TariffCategories { get; }
    DbSet<HierarchyNode> HierarchyNodes { get; }
    DbSet<OrgUnit> OrgUnits { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<VeeExecutionRecord> VeeExecutionRecords { get; }
    DbSet<NetworkEnergyReading> NetworkEnergyReadings { get; }
    DbSet<Complaint> Complaints { get; }
    DbSet<MeterInventoryRecord> MeterInventoryRecords { get; }
    DbSet<InstallationQualityCheck> InstallationQualityChecks { get; }
    DbSet<RevenueProtectionLead> RevenueProtectionLeads { get; }
    DbSet<RiskSignal> RiskSignals { get; }
    DbSet<PrepaidAccount> PrepaidAccounts { get; }
    DbSet<WalletTransaction> WalletTransactions { get; }
    DbSet<SavedFilter> SavedFilters { get; }
    DbSet<DownloadRequest> DownloadRequests { get; }
    DbSet<UserTenantAccess> UserTenantAccesses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
