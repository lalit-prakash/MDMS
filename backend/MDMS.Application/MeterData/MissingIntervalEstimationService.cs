using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Application.MeterData;

/// <summary>
/// Detects missing 30-minute Load Survey slots for a meter/day (48 expected) and, where both
/// immediate neighbors are available, estimates the gap using the "average of surrounding
/// periods" method — one of the estimation approaches this project's spec explicitly allows.
/// Every attempt, successful or not, is recorded as a <see cref="VeeExecutionRecord"/> so the
/// outcome is explainable later; a slot with fewer than two usable neighbors is never guessed at
/// or silently treated as zero — it stays missing and the record says why.
/// </summary>
public class MissingIntervalEstimationService
{
    private const string RuleName = "AverageOfSurroundingPeriods";
    private static readonly TimeSpan SlotLength = TimeSpan.FromMinutes(30);

    private readonly IMdmsDbContext _db;

    public MissingIntervalEstimationService(IMdmsDbContext db)
    {
        _db = db;
    }

    /// <summary>The 48 expected 30-minute slot boundaries for <paramref name="date"/> (UTC day boundary).</summary>
    public static IReadOnlyList<(DateTime Start, DateTime End)> ExpectedSlots(DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var slots = new List<(DateTime, DateTime)>(48);
        for (var i = 0; i < 48; i++)
        {
            var start = dayStart + SlotLength * i;
            slots.Add((start, start + SlotLength));
        }
        return slots;
    }

    /// <summary>Slots for this meter/day with no <see cref="LoadSurveyInterval"/> row at all (any quality).</summary>
    public async Task<IReadOnlyList<(DateTime Start, DateTime End)>> DetectMissingSlotsAsync(
        Guid meterId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var slots = ExpectedSlots(date);
        var dayStart = slots[0].Start;
        var dayEnd = slots[^1].End;

        var existing = await _db.LoadSurveyIntervals
            .Where(i => i.MeterId == meterId && i.IntervalStartUtc >= dayStart && i.IntervalStartUtc < dayEnd)
            .Select(i => i.IntervalStartUtc)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet();
        return slots.Where(s => !existingSet.Contains(s.Start)).ToList();
    }

    /// <summary>
    /// Attempts to estimate every missing slot for this meter/day. Returns the audit record for
    /// every slot attempted (both successful estimates and documented skips).
    /// </summary>
    public async Task<IReadOnlyList<VeeExecutionRecord>> EstimateMissingSlotsAsync(
        Guid meterId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var missingSlots = await DetectMissingSlotsAsync(meterId, date, cancellationToken);
        var records = new List<VeeExecutionRecord>();

        foreach (var (start, end) in missingSlots)
        {
            var previous = await _db.LoadSurveyIntervals
                .Where(i => i.MeterId == meterId && i.IntervalEndUtc == start && i.Quality == MeasurementQuality.Valid)
                .FirstOrDefaultAsync(cancellationToken);

            var next = await _db.LoadSurveyIntervals
                .Where(i => i.MeterId == meterId && i.IntervalStartUtc == end && i.Quality == MeasurementQuality.Valid)
                .FirstOrDefaultAsync(cancellationToken);

            if (previous is null || next is null)
            {
                var record = VeeExecutionRecord.Record(
                    RuleName, MeasurementRangeType.LoadSurveyInterval, meterId, start, end,
                    MeasurementQuality.Missing, newValue: null,
                    details: "Insufficient neighboring valid intervals to estimate — left missing rather than guessed at.");
                _db.VeeExecutionRecords.Add(record);
                records.Add(record);
                continue;
            }

            var estimatedConsumption = (previous.ConsumptionKwh + next.ConsumptionKwh) / 2m;
            var estimatedCumulative = previous.CumulativeReading + estimatedConsumption;

            var estimated = LoadSurveyInterval.CreateValid(
                meterId, start, end, estimatedCumulative, estimatedConsumption, MeasurementSource.Estimated);
            _db.LoadSurveyIntervals.Add(estimated);

            var appliedRecord = VeeExecutionRecord.Record(
                RuleName, MeasurementRangeType.LoadSurveyInterval, meterId, start, end,
                MeasurementQuality.Valid, estimatedConsumption,
                $"Estimated as the average of the surrounding intervals' consumption ({previous.ConsumptionKwh} and {next.ConsumptionKwh} kWh).");
            _db.VeeExecutionRecords.Add(appliedRecord);
            records.Add(appliedRecord);
        }

        if (records.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return records;
    }
}
