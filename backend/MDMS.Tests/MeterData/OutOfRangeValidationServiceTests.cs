using MDMS.Application.MeterData;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Tests.MeterData;

public class OutOfRangeValidationServiceTests
{
    private static MdmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MdmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MdmsDbContext(options);
    }

    private static LoadSurveyInterval AddValidInterval(MdmsDbContext db, Guid meterId, decimal consumptionKwh)
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var interval = LoadSurveyInterval.CreateValid(
            meterId, start, start.AddMinutes(30), consumptionKwh, consumptionKwh, MeasurementSource.Received);
        db.LoadSurveyIntervals.Add(interval);
        return interval;
    }

    private static DailyLoadProfile AddValidProfile(MdmsDbContext db, Guid meterId, decimal consumptionKwh)
        => AddValidProfile(db, Guid.NewGuid(), meterId, consumptionKwh);

    private static DailyLoadProfile AddValidProfile(MdmsDbContext db, Guid servicePointId, Guid meterId, decimal consumptionKwh)
    {
        var profile = DailyLoadProfile.CreateReceived(servicePointId, meterId, new DateOnly(2026, 1, 1), consumptionKwh);
        db.DailyLoadProfiles.Add(profile);
        return profile;
    }

    [Fact]
    public async Task GetEffectiveThresholdAsync_MeterSpecificTakesPrecedenceOverGlobal()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, null, 0m, 10m));
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, meterId, 0m, 100m));
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var effective = await service.GetEffectiveThresholdAsync(meterId, MeasurementRangeType.LoadSurveyInterval);

        Assert.NotNull(effective);
        Assert.Equal(meterId, effective!.MeterId);
        Assert.Equal(100m, effective.MaxConsumptionKwh);
    }

    [Fact]
    public async Task GetEffectiveThresholdAsync_NoMeterSpecific_FallsBackToGlobal()
    {
        await using var db = CreateContext();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, null, 0m, 10m));
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var effective = await service.GetEffectiveThresholdAsync(Guid.NewGuid(), MeasurementRangeType.LoadSurveyInterval);

        Assert.NotNull(effective);
        Assert.Null(effective!.MeterId);
    }

    [Fact]
    public async Task GetEffectiveThresholdAsync_NoThresholdConfigured_ReturnsNull()
    {
        await using var db = CreateContext();
        var service = new OutOfRangeValidationService(db);

        var effective = await service.GetEffectiveThresholdAsync(Guid.NewGuid(), MeasurementRangeType.LoadSurveyInterval);

        Assert.Null(effective);
    }

    [Fact]
    public async Task GetEffectiveThresholdAsync_TypesAreLookedUpIndependently()
    {
        // An LS threshold for a meter must never satisfy a DLP lookup for the same meter, and
        // vice versa — the two measurement products' plausible ranges are entirely different scales.
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, meterId, 0m, 50m));
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var dlpThreshold = await service.GetEffectiveThresholdAsync(meterId, MeasurementRangeType.DailyLoadProfile);

        Assert.Null(dlpThreshold);
    }

    [Fact]
    public async Task RunLoadSurveyCheckAsync_ConsumptionAboveMax_IsFlaggedOutOfRange()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, meterId, 0m, 50m));
        AddValidInterval(db, meterId, 75m);
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var flagged = await service.RunLoadSurveyCheckAsync(meterId);

        Assert.Single(flagged);
        var stored = await db.LoadSurveyIntervals.SingleAsync();
        Assert.Equal(MeasurementQuality.OutOfRange, stored.Quality);
        Assert.Equal(75m, stored.ConsumptionKwh); // the value itself is never altered
    }

    [Fact]
    public async Task RunLoadSurveyCheckAsync_ConsumptionWithinRange_IsNotFlagged()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, meterId, 0m, 50m));
        AddValidInterval(db, meterId, 20m);
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var flagged = await service.RunLoadSurveyCheckAsync(meterId);

        Assert.Empty(flagged);
        var stored = await db.LoadSurveyIntervals.SingleAsync();
        Assert.Equal(MeasurementQuality.Valid, stored.Quality);
    }

    [Fact]
    public async Task RunLoadSurveyCheckAsync_NoThresholdConfiguredForMeter_NeverFlags()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        AddValidInterval(db, meterId, 1_000_000m); // implausible, but no threshold means no opinion
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var flagged = await service.RunLoadSurveyCheckAsync(meterId);

        Assert.Empty(flagged);
    }

    [Fact]
    public async Task RunLoadSurveyCheckAsync_WithoutMeterFilter_SweepsEveryMeterWithAThreshold()
    {
        await using var db = CreateContext();
        var meterWithThreshold = Guid.NewGuid();
        var meterWithoutThreshold = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, meterWithThreshold, 0m, 10m));
        AddValidInterval(db, meterWithThreshold, 50m);
        AddValidInterval(db, meterWithoutThreshold, 50m);
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var flagged = await service.RunLoadSurveyCheckAsync();

        var flaggedInterval = Assert.Single(flagged);
        Assert.Equal(meterWithThreshold, flaggedInterval.MeterId);
    }

    [Fact]
    public async Task RunDailyLoadProfileCheckAsync_ConsumptionAboveMax_IsFlaggedOutOfRange()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(MeasurementRangeType.DailyLoadProfile, meterId, 0m, 100m));
        AddValidProfile(db, meterId, 500m);
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var flagged = await service.RunDailyLoadProfileCheckAsync(meterId);

        Assert.Single(flagged);
        var stored = await db.DailyLoadProfiles.SingleAsync();
        Assert.Equal(MeasurementQuality.OutOfRange, stored.Quality);
    }

    [Fact]
    public async Task RunDailyLoadProfileCheckAsync_DoesNotTouchLoadSurveyIntervals()
    {
        // Guards against the two check paths bleeding into each other's tables.
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(MeasurementRangeType.DailyLoadProfile, meterId, 0m, 10m));
        AddValidInterval(db, meterId, 999m); // would breach if the DLP threshold were mistakenly applied here
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var flagged = await service.RunDailyLoadProfileCheckAsync(meterId);

        Assert.Empty(flagged);
        var storedInterval = await db.LoadSurveyIntervals.SingleAsync();
        Assert.Equal(MeasurementQuality.Valid, storedInterval.Quality);
    }
}
