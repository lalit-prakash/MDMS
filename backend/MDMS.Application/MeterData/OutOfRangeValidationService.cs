using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Application.MeterData;

/// <summary>
/// Evaluates consumption against configurable plausibility thresholds
/// (<see cref="MeasurementRangeThreshold"/>) — the "out-of-range" half of MDMS's VEE
/// responsibility, alongside <see cref="LoadSurveyIngestionService"/>'s and
/// <see cref="DailyLoadProfileIngestionService"/>'s negative-consumption/overwrite checks.
/// Covers both Load Survey intervals and Daily Load Profiles, kept separate via
/// <see cref="MeasurementRangeType"/> since their plausible ranges are entirely different scales.
/// A meter-specific threshold takes precedence over the global default for that type; a meter
/// with neither configured is never flagged (no threshold means no opinion, not a false positive).
/// </summary>
public class OutOfRangeValidationService
{
    private readonly IMdmsDbContext _db;

    public OutOfRangeValidationService(IMdmsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the threshold that applies to <paramref name="meterId"/> for <paramref name="measurementType"/>:
    /// a meter-specific one if configured, otherwise the global default (<c>MeterId == null</c>)
    /// for that same type, otherwise <c>null</c>.
    /// </summary>
    public async Task<MeasurementRangeThreshold?> GetEffectiveThresholdAsync(
        Guid meterId, MeasurementRangeType measurementType, CancellationToken cancellationToken = default)
    {
        var meterSpecific = await _db.MeasurementRangeThresholds
            .Where(t => t.MeasurementType == measurementType && t.MeterId == meterId && t.IsActive)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (meterSpecific is not null)
            return meterSpecific;

        return await _db.MeasurementRangeThresholds
            .Where(t => t.MeasurementType == measurementType && t.MeterId == null && t.IsActive)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Re-evaluates every currently-Valid Load Survey interval for a meter (or every meter with a
    /// configured LS threshold, if <paramref name="meterId"/> is omitted) against its effective
    /// threshold, flagging any breach via <see cref="LoadSurveyInterval.FlagOutOfRange"/>.
    /// </summary>
    public async Task<IReadOnlyList<LoadSurveyInterval>> RunLoadSurveyCheckAsync(
        Guid? meterId = null, CancellationToken cancellationToken = default)
    {
        var meterIds = await ResolveCandidateMeterIdsAsync(
            MeasurementRangeType.LoadSurveyInterval, meterId,
            () => _db.LoadSurveyIntervals.Where(i => i.Quality == MeasurementQuality.Valid).Select(i => i.MeterId),
            cancellationToken);

        var flagged = new List<LoadSurveyInterval>();

        foreach (var currentMeterId in meterIds)
        {
            var threshold = await GetEffectiveThresholdAsync(
                currentMeterId, MeasurementRangeType.LoadSurveyInterval, cancellationToken);
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

    /// <summary>
    /// Re-evaluates every currently-Valid Daily Load Profile for a meter (or every meter with a
    /// configured DLP threshold, if <paramref name="meterId"/> is omitted) against its effective
    /// threshold, flagging any breach via <see cref="DailyLoadProfile.FlagOutOfRange"/>.
    /// </summary>
    public async Task<IReadOnlyList<DailyLoadProfile>> RunDailyLoadProfileCheckAsync(
        Guid? meterId = null, CancellationToken cancellationToken = default)
    {
        var meterIds = await ResolveCandidateMeterIdsAsync(
            MeasurementRangeType.DailyLoadProfile, meterId,
            () => _db.DailyLoadProfiles.Where(p => p.Quality == MeasurementQuality.Valid).Select(p => p.MeterId),
            cancellationToken);

        var flagged = new List<DailyLoadProfile>();

        foreach (var currentMeterId in meterIds)
        {
            var threshold = await GetEffectiveThresholdAsync(
                currentMeterId, MeasurementRangeType.DailyLoadProfile, cancellationToken);
            if (threshold is null)
                continue;

            var candidates = await _db.DailyLoadProfiles
                .Where(p => p.MeterId == currentMeterId && p.Quality == MeasurementQuality.Valid)
                .ToListAsync(cancellationToken);

            foreach (var profile in candidates.Where(p => !threshold.IsWithinRange(p.ConsumptionKwh)))
            {
                profile.FlagOutOfRange();
                flagged.Add(profile);
            }
        }

        if (flagged.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return flagged;
    }

    /// <summary>
    /// With an explicit meter, that's the only candidate. Otherwise: any meter with its own
    /// threshold of this type is a candidate; if a global default for this type also exists, every
    /// meter with at least one Valid measurement of this type becomes a candidate too.
    /// </summary>
    private async Task<List<Guid>> ResolveCandidateMeterIdsAsync(
        MeasurementRangeType measurementType, Guid? meterId,
        Func<IQueryable<Guid>> allMeterIdsWithValidMeasurements, CancellationToken cancellationToken)
    {
        if (meterId is Guid id)
            return new List<Guid> { id };

        var meterIds = await _db.MeasurementRangeThresholds
            .Where(t => t.MeasurementType == measurementType && t.MeterId != null && t.IsActive)
            .Select(t => t.MeterId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var hasGlobalThreshold = await _db.MeasurementRangeThresholds
            .AnyAsync(t => t.MeasurementType == measurementType && t.MeterId == null && t.IsActive, cancellationToken);

        if (hasGlobalThreshold)
            meterIds = await allMeterIdsWithValidMeasurements().Distinct().ToListAsync(cancellationToken);

        return meterIds;
    }
}
