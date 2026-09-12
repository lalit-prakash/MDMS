using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Application.Common;

/// <summary>
/// The persistence seam the Application layer depends on, implemented by Infrastructure's
/// EF Core <c>MdmsDbContext</c>. Keeps Application free of a direct EF Core/Npgsql dependency,
/// matching prepaid_engine's own layering discipline.
/// </summary>
public interface IMdmsDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<ServicePoint> ServicePoints { get; }
    DbSet<Meter> Meters { get; }
    DbSet<MeterAssignment> MeterAssignments { get; }
    DbSet<LoadSurveyInterval> LoadSurveyIntervals { get; }
    DbSet<DailyLoadProfile> DailyLoadProfiles { get; }
    DbSet<DataQualityHold> DataQualityHolds { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
