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

    [Fact]
    public async Task GetEffectiveThresholdAsync_MeterSpecificTakesPrecedenceOverGlobal()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(null, 0m, 10m));
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(meterId, 0m, 100m));
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var effective = await service.GetEffectiveThresholdAsync(meterId);

        Assert.NotNull(effective);
        Assert.Equal(meterId, effective!.MeterId);
        Assert.Equal(100m, effective.MaxConsumptionKwh);
    }

    [Fact]
    public async Task GetEffectiveThresholdAsync_NoMeterSpecific_FallsBackToGlobal()
    {
        await using var db = CreateContext();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(null, 0m, 10m));
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var effective = await service.GetEffectiveThresholdAsync(Guid.NewGuid());

        Assert.NotNull(effective);
        Assert.Null(effective!.MeterId);
    }

    [Fact]
    public async Task GetEffectiveThresholdAsync_NoThresholdConfigured_ReturnsNull()
    {
        await using var db = CreateContext();
        var service = new OutOfRangeValidationService(db);

        var effective = await service.GetEffectiveThresholdAsync(Guid.NewGuid());

        Assert.Null(effective);
    }

    [Fact]
    public async Task RunCheckAsync_ConsumptionAboveMax_IsFlaggedOutOfRange()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(meterId, 0m, 50m));
        AddValidInterval(db, meterId, 75m);
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var flagged = await service.RunCheckAsync(meterId);

        Assert.Single(flagged);
        var stored = await db.LoadSurveyIntervals.SingleAsync();
        Assert.Equal(MeasurementQuality.OutOfRange, stored.Quality);
        Assert.Equal(75m, stored.ConsumptionKwh); // the value itself is never altered
    }

    [Fact]
    public async Task RunCheckAsync_ConsumptionWithinRange_IsNotFlagged()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(meterId, 0m, 50m));
        AddValidInterval(db, meterId, 20m);
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var flagged = await service.RunCheckAsync(meterId);

        Assert.Empty(flagged);
        var stored = await db.LoadSurveyIntervals.SingleAsync();
        Assert.Equal(MeasurementQuality.Valid, stored.Quality);
    }

    [Fact]
    public async Task RunCheckAsync_NoThresholdConfiguredForMeter_NeverFlags()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        AddValidInterval(db, meterId, 1_000_000m); // implausible, but no threshold means no opinion
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var flagged = await service.RunCheckAsync(meterId);

        Assert.Empty(flagged);
    }

    [Fact]
    public async Task RunCheckAsync_WithoutMeterFilter_SweepsEveryMeterWithAThreshold()
    {
        await using var db = CreateContext();
        var meterWithThreshold = Guid.NewGuid();
        var meterWithoutThreshold = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(meterWithThreshold, 0m, 10m));
        AddValidInterval(db, meterWithThreshold, 50m);
        AddValidInterval(db, meterWithoutThreshold, 50m);
        await db.SaveChangesAsync();

        var service = new OutOfRangeValidationService(db);
        var flagged = await service.RunCheckAsync();

        var flaggedInterval = Assert.Single(flagged);
        Assert.Equal(meterWithThreshold, flaggedInterval.MeterId);
    }
}
