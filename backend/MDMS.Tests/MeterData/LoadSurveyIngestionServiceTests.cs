using MDMS.Application.MeterData;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Tests.MeterData;

public class LoadSurveyIngestionServiceTests
{
    private static MdmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MdmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MdmsDbContext(options);
    }

    [Fact]
    public async Task IngestAsync_FirstIntervalForMeter_HasZeroConsumption_AndIsValid()
    {
        await using var db = CreateContext();
        var service = new LoadSurveyIngestionService(db, new OutOfRangeValidationService(db));
        var meterId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var results = await service.IngestAsync(new[]
        {
            new LoadSurveyIngestRequest(meterId, start, start.AddMinutes(30), 100m)
        });

        Assert.Single(results);
        Assert.Equal(MeasurementQuality.Valid, results[0].Quality);

        var stored = await db.LoadSurveyIntervals.SingleAsync();
        Assert.Equal(0m, stored.ConsumptionKwh);
    }

    [Fact]
    public async Task IngestAsync_IncreasingReadings_ComputesConsumptionDelta()
    {
        await using var db = CreateContext();
        var service = new LoadSurveyIngestionService(db, new OutOfRangeValidationService(db));
        var meterId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await service.IngestAsync(new[]
        {
            new LoadSurveyIngestRequest(meterId, start, start.AddMinutes(30), 100m)
        });
        var second = await service.IngestAsync(new[]
        {
            new LoadSurveyIngestRequest(meterId, start.AddMinutes(30), start.AddMinutes(60), 105m)
        });

        Assert.Equal(MeasurementQuality.Valid, second[0].Quality);
        var stored = await db.LoadSurveyIntervals
            .OrderByDescending(i => i.IntervalEndUtc)
            .FirstAsync();
        Assert.Equal(5m, stored.ConsumptionKwh);
    }

    [Fact]
    public async Task IngestAsync_NegativeConsumption_IsRejected_AndRaisesHold()
    {
        await using var db = CreateContext();
        var service = new LoadSurveyIngestionService(db, new OutOfRangeValidationService(db));
        var meterId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await service.IngestAsync(new[]
        {
            new LoadSurveyIngestRequest(meterId, start, start.AddMinutes(30), 100m)
        });
        var second = await service.IngestAsync(new[]
        {
            new LoadSurveyIngestRequest(meterId, start.AddMinutes(30), start.AddMinutes(60), 90m)
        });

        Assert.Equal(MeasurementQuality.NegativeConsumption, second[0].Quality);

        var hold = await db.DataQualityHolds.SingleAsync();
        Assert.True(hold.IsActive);
        Assert.Equal(meterId, hold.MeterId);
    }

    [Fact]
    public async Task IngestAsync_TwoBlocksInOneBatch_SecondNegative_IsDetectedWithinBatch()
    {
        // Regression test: a negative-consumption sequence within one in-flight batch must be
        // caught even though neither row is persisted yet when the second is evaluated.
        await using var db = CreateContext();
        var service = new LoadSurveyIngestionService(db, new OutOfRangeValidationService(db));
        var meterId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var results = await service.IngestAsync(new[]
        {
            new LoadSurveyIngestRequest(meterId, start, start.AddMinutes(30), 100m),
            new LoadSurveyIngestRequest(meterId, start.AddMinutes(30), start.AddMinutes(60), 80m)
        });

        Assert.Equal(MeasurementQuality.Valid, results[0].Quality);
        Assert.Equal(MeasurementQuality.NegativeConsumption, results[1].Quality);
    }

    [Fact]
    public async Task IngestAsync_DuplicateIntervalInSameBatch_IsIngestedOnce()
    {
        await using var db = CreateContext();
        var service = new LoadSurveyIngestionService(db, new OutOfRangeValidationService(db));
        var meterId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var request = new LoadSurveyIngestRequest(meterId, start, start.AddMinutes(30), 100m);

        var results = await service.IngestAsync(new[] { request, request });

        Assert.Single(results);
        Assert.Equal(1, await db.LoadSurveyIntervals.CountAsync());
    }

    [Fact]
    public async Task IngestAsync_AlreadyPersistedInterval_IsIdempotent()
    {
        await using var db = CreateContext();
        var service = new LoadSurveyIngestionService(db, new OutOfRangeValidationService(db));
        var meterId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var request = new LoadSurveyIngestRequest(meterId, start, start.AddMinutes(30), 100m);

        await service.IngestAsync(new[] { request });
        var second = await service.IngestAsync(new[] { request });

        Assert.Empty(second);
        Assert.Equal(1, await db.LoadSurveyIntervals.CountAsync());
    }

    [Fact]
    public async Task IngestAsync_ConsumptionAboveConfiguredThreshold_IsFlaggedOutOfRangeInline()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        db.MeasurementRangeThresholds.Add(new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, meterId, 0m, 50m));
        await db.SaveChangesAsync();

        var service = new LoadSurveyIngestionService(db, new OutOfRangeValidationService(db));
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await service.IngestAsync(new[]
        {
            new LoadSurveyIngestRequest(meterId, start, start.AddMinutes(30), 100m)
        });
        var second = await service.IngestAsync(new[]
        {
            new LoadSurveyIngestRequest(meterId, start.AddMinutes(30), start.AddMinutes(60), 200m) // +100 kWh, above the 50 kWh max
        });

        Assert.Equal(MeasurementQuality.OutOfRange, second[0].Quality);

        // Still trusted as the sequence anchor for the next interval — not a hold-raising rejection.
        var third = await service.IngestAsync(new[]
        {
            new LoadSurveyIngestRequest(meterId, start.AddMinutes(60), start.AddMinutes(90), 210m)
        });
        Assert.Equal(MeasurementQuality.Valid, third[0].Quality);
        Assert.Empty(await db.DataQualityHolds.ToListAsync());
    }

    [Fact]
    public async Task IngestAsync_NoThresholdConfigured_NeverFlagsOutOfRange()
    {
        await using var db = CreateContext();
        var service = new LoadSurveyIngestionService(db, new OutOfRangeValidationService(db));
        var meterId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var results = await service.IngestAsync(new[]
        {
            new LoadSurveyIngestRequest(meterId, start, start.AddMinutes(30), 1_000_000m)
        });

        Assert.Equal(MeasurementQuality.Valid, results[0].Quality);
    }
}
