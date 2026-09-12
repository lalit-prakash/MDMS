using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Application.MeterData;

/// <summary>
/// Evaluates Load Survey consumption against configurable plausibility thresholds
/// (<see cref="MeasurementRangeThreshold"/>) — the "out-of-range" half of MDMS's VEE
/// responsibility, alongside <see cref="LoadSurveyIngestionService"/>'s negative-consumption
/// checks. A meter-specific threshold takes precedence over the global default; a meter with
/// neither configured is never flagged (no threshold means no opinion, not a false positive).
/// </summary>
public class OutOfRangeValidationService
{
    private readonly IMdmsDbContext _db;

    public OutOfRangeValidationService(IMdmsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the threshold that applies to <paramref name="meterId"/>: a meter-specific one if
    /// configured, otherwise the global default (<c>MeterId == null</c>), otherwise <c>null</c>.
    /// </summary>
    public async Task<MeasurementRangeThreshold?> GetEffectiveThresholdAsync(
        Guid meterId, CancellationToken cancellationToken = default)
    {
        var meterSpecific = await _db.MeasurementRangeThresholds
            .Where(t => t.MeterId == meterId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (meterSpecific is not null)
            return meterSpecific;

        return await _db.MeasurementRangeThresholds
            .Where(t => t.MeterId == null)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Re-evaluates every currently-Valid Load Survey interval for a meter (or every meter with a
    /// configured threshold, if <paramref name="meterId"/> is omitted) against its effective
    /// threshold, flagging any breach via <see cref="LoadSurveyInterval.FlagOutOfRange"/>. Useful
    /// both as a one-off sweep after a threshold is added/changed, and as the engine behind the
    /// out-of-range check endpoint.
    /// </summary>
    public async Task<IReadOnlyList<LoadSurveyInterval>> RunCheckAsync(
        Guid? meterId = null, CancellationToken cancellationToken = default)
    {
        var meterIds = meterId is Guid id
            ? new List<Guid> { id }
            : await _db.MeasurementRangeThresholds
                .Where(t => t.MeterId != null)
                .Select(t => t.MeterId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

        var globalThreshold = await _db.MeasurementRangeThresholds
            .Where(t => t.MeterId == null)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        // With no meter filter and no global threshold, only meters with their own configured
        // threshold are worth scanning at all — meterIds already reflects exactly that set.
        if (meterId is null && globalThreshold is not null)
        {
            var allMeterIds = await _db.LoadSurveyIntervals
                .Where(i => i.Quality == MeasurementQuality.Valid)
                .Select(i => i.MeterId)
                .Distinct()
                .ToListAsync(cancellationToken);
            meterIds = allMeterIds;
        }

        var flagged = new List<LoadSurveyInterval>();

        foreach (var currentMeterId in meterIds)
        {
            var threshold = await GetEffectiveThresholdAsync(currentMeterId, cancellationToken);
            if (threshold is null)
                continue;

            var candidates = await _db.LoadSurveyIntervals
                .Where(i => i.MeterId == currentMeterId && i.Quality == MeasurementQuality.Valid)
                .ToListAsync(cancellationToken);

            foreach (var interval in candidates.Where(i => !threshold.IsWithinRange(i.ConsumptionKwh)))
            {
                interval.FlagOutOfRange();
                flagged.Add(interval);
            }
        }

        if (flagged.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return flagged;
    }
}
