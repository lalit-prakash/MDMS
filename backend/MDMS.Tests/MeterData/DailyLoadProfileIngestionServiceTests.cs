using MDMS.Application.MeterData;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Tests.MeterData;

public class DailyLoadProfileIngestionServiceTests
{
    private static MdmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MdmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MdmsDbContext(options);
    }

    [Fact]
    public async Task IngestAsync_NoExistingProfile_CreatesReceivedProfile()
    {
        await using var db = CreateContext();
        var service = new DailyLoadProfileIngestionService(db, new OutOfRangeValidationService(db));
        var servicePointId = Guid.NewGuid();
        var meterId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 1);

        var result = await service.IngestAsync(
            new DailyLoadProfileIngestRequest(servicePointId, meterId, date, 42.5m));

        Assert.Equal(MeasurementSource.Received, result.Source);
        Assert.False(result.ReplacedProvisional);
        Assert.Equal(42.5m, result.ConsumptionKwh);

        var stored = await db.DailyLoadProfiles.SingleAsync();
        Assert.Equal(MeasurementQuality.Valid, stored.Quality);
    }

    [Fact]
    public async Task IngestAsync_ProvisionalExists_IsReplacedByReceived()
    {
        await using var db = CreateContext();
        var servicePointId = Guid.NewGuid();
        var meterId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 1);

        db.DailyLoadProfiles.Add(DailyLoadProfile.CreateProvisional(servicePointId, meterId, date, 10m));
        await db.SaveChangesAsync();

        var service = new DailyLoadProfileIngestionService(db, new OutOfRangeValidationService(db));
        var result = await service.IngestAsync(
            new DailyLoadProfileIngestRequest(servicePointId, meterId, date, 37m));

        Assert.True(result.ReplacedProvisional);
        Assert.Equal(MeasurementSource.Received, result.Source);
        Assert.Equal(37m, result.ConsumptionKwh);

        var profiles = await db.DailyLoadProfiles.ToListAsync();
        var stillPresent = Assert.Single(profiles);
        Assert.Equal(MeasurementSource.Received, stillPresent.Source);
        Assert.Equal(37m, stillPresent.ConsumptionKwh);
    }

    [Fact]
    public async Task IngestAsync_ReceivedProfileAlreadyExists_ThrowsAndDoesNotOverwrite()
    {
        await using var db = CreateContext();
        var servicePointId = Guid.NewGuid();
        var meterId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 1);

        db.DailyLoadProfiles.Add(DailyLoadProfile.CreateReceived(servicePointId, meterId, date, 50m));
        await db.SaveChangesAsync();

        var service = new DailyLoadProfileIngestionService(db, new OutOfRangeValidationService(db));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.IngestAsync(new DailyLoadProfileIngestRequest(servicePointId, meterId, date, 999m)));

        var stored = await db.DailyLoadProfiles.SingleAsync();
        Assert.Equal(50m, stored.ConsumptionKwh);
    }

    [Fact]
    public async Task IngestAsync_ConsumptionAboveConfiguredThreshold_IsFlaggedOutOfRangeInline()
    {
        await using var db = CreateContext();
        var servicePointId = Guid.NewGuid();
        var meterId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 1);

        db.MeasurementRangeThresholds.Add(
            new MeasurementRangeThreshold(MeasurementRangeType.DailyLoadProfile, meterId, 0m, 100m));
        await db.SaveChangesAsync();

        var service = new DailyLoadProfileIngestionService(db, new OutOfRangeValidationService(db));
        var result = await service.IngestAsync(
            new DailyLoadProfileIngestRequest(servicePointId, meterId, date, 500m));

        Assert.Equal(MeasurementQuality.OutOfRange, result.Quality);
        var stored = await db.DailyLoadProfiles.SingleAsync();
        Assert.Equal(MeasurementQuality.OutOfRange, stored.Quality);
        Assert.Equal(500m, stored.ConsumptionKwh); // the value itself is never altered
    }

    [Fact]
    public async Task IngestAsync_NoThresholdConfigured_NeverFlagsOutOfRange()
    {
        await using var db = CreateContext();
        var service = new DailyLoadProfileIngestionService(db, new OutOfRangeValidationService(db));

        var result = await service.IngestAsync(
            new DailyLoadProfileIngestRequest(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), 1_000_000m));

        Assert.Equal(MeasurementQuality.Valid, result.Quality);
    }

    [Fact]
    public async Task IngestAsync_LsThresholdDoesNotApplyToDlp()
    {
        // An LS threshold configured for the same meter must never leak into DLP evaluation —
        // the two measurement types are looked up independently via MeasurementRangeType.
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(
            new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, meterId, 0m, 10m));
        await db.SaveChangesAsync();

        var service = new DailyLoadProfileIngestionService(db, new OutOfRangeValidationService(db));
        var result = await service.IngestAsync(
            new DailyLoadProfileIngestRequest(Guid.NewGuid(), meterId, new DateOnly(2026, 1, 1), 500m));

        Assert.Equal(MeasurementQuality.Valid, result.Quality);
    }
}
