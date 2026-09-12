using MDMS.Application.MeterData;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Tests.MeterData;

public class MissingIntervalEstimationServiceTests
{
    private static MdmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MdmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MdmsDbContext(options);
    }

    private static void AddValidInterval(
        MdmsDbContext db, Guid meterId, DateTime start, DateTime end, decimal cumulative, decimal consumption)
    {
        db.LoadSurveyIntervals.Add(
            LoadSurveyInterval.CreateValid(meterId, start, end, cumulative, consumption, MeasurementSource.Received));
    }

    [Fact]
    public void ExpectedSlots_ReturnsFortyEightHalfHourSlotsForTheDay()
    {
        var slots = MissingIntervalEstimationService.ExpectedSlots(new DateOnly(2026, 1, 1));

        Assert.Equal(48, slots.Count);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), slots[0].Start);
        Assert.Equal(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), slots[^1].End);
    }

    [Fact]
    public async Task DetectMissingSlotsAsync_NoIntervalsAtAll_AllFortyEightAreMissing()
    {
        await using var db = CreateContext();
        var service = new MissingIntervalEstimationService(db);

        var missing = await service.DetectMissingSlotsAsync(Guid.NewGuid(), new DateOnly(2026, 1, 1));

        Assert.Equal(48, missing.Count);
    }

    [Fact]
    public async Task DetectMissingSlotsAsync_OneSlotMissing_ReturnsOnlyThatSlot()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 1);
        var slots = MissingIntervalEstimationService.ExpectedSlots(date);

        foreach (var (start, end) in slots)
        {
            if (start == new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc))
                continue; // leave the 10:00-10:30 slot missing

            AddValidInterval(db, meterId, start, end, 100m, 1m);
        }
        await db.SaveChangesAsync();

        var service = new MissingIntervalEstimationService(db);
        var missing = await service.DetectMissingSlotsAsync(meterId, date);

        var slot = Assert.Single(missing);
        Assert.Equal(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc), slot.Start);
    }

    [Fact]
    public async Task EstimateMissingSlotsAsync_BothNeighborsPresent_EstimatesAverageAndPersists()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 1);
        var slotStart = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var slotEnd = slotStart.AddMinutes(30);

        AddValidInterval(db, meterId, slotStart.AddMinutes(-30), slotStart, 100m, 4m); // previous: 4 kWh
        AddValidInterval(db, meterId, slotEnd, slotEnd.AddMinutes(30), 108m, 6m);      // next: 6 kWh
        await db.SaveChangesAsync();

        var service = new MissingIntervalEstimationService(db);
        var records = await service.EstimateMissingSlotsAsync(meterId, date);

        var record = Assert.Single(records.Where(r => r.SlotStartUtc == slotStart));
        Assert.Equal(MeasurementQuality.Valid, record.ResultQuality);
        Assert.Equal(5m, record.NewValue); // average of 4 and 6

        var stored = await db.LoadSurveyIntervals.SingleAsync(i => i.IntervalStartUtc == slotStart);
        Assert.Equal(MeasurementSource.Estimated, stored.Source);
        Assert.Equal(5m, stored.ConsumptionKwh);
        Assert.Equal(105m, stored.CumulativeReading); // previous cumulative (100) + estimated 5
    }

    [Fact]
    public async Task EstimateMissingSlotsAsync_MissingNeighbor_NeverGuesses_RecordsAsStillMissing()
    {
        await using var db = CreateContext();
        var meterId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 1);
        // No intervals at all — every slot is missing with no neighbors on either side.

        var service = new MissingIntervalEstimationService(db);
        var records = await service.EstimateMissingSlotsAsync(meterId, date);

        Assert.Equal(48, records.Count);
        Assert.All(records, r => Assert.Equal(MeasurementQuality.Missing, r.ResultQuality));
        Assert.Empty(await db.LoadSurveyIntervals.ToListAsync());
    }
}
