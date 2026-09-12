using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Application.MeterData;

/// <summary>
/// Validates and stores incoming Load Survey blocks, applying strict sequence-continuity
/// discipline: the last-known cumulative reading per meter is tracked across the whole
/// in-flight batch, not just already-saved rows, so two blocks for the same meter in one request
/// cannot both incorrectly come back Valid when the second is actually a negative-consumption
/// event. Also applies <see cref="OutOfRangeValidationService"/>'s
/// plausibility check inline, so an implausible reading is flagged the moment it arrives rather
/// than only on the next on-demand sweep.
/// </summary>
public class LoadSurveyIngestionService
{
    private readonly IMdmsDbContext _db;
    private readonly OutOfRangeValidationService _outOfRangeValidationService;

    public LoadSurveyIngestionService(IMdmsDbContext db, OutOfRangeValidationService outOfRangeValidationService)
    {
        _db = db;
        _outOfRangeValidationService = outOfRangeValidationService;
    }

    public async Task<IReadOnlyList<LoadSurveyIngestResult>> IngestAsync(
        IReadOnlyList<LoadSurveyIngestRequest> requests, CancellationToken cancellationToken = default)
    {
        var results = new List<LoadSurveyIngestResult>();

        // Seed "last known cumulative reading per meter" from persisted data, then keep it
        // updated in-memory as this batch is processed — never re-querying only saved rows.
        // Deliberately excludes only NegativeConsumption (the one quality whose CumulativeReading
        // is untrusted) — an OutOfRange interval's reading is still a trustworthy sequence anchor,
        // only its consumption delta was implausible.
        var meterIds = requests.Select(r => r.MeterId).Distinct().ToList();
        var lastReadingByMeter = await _db.LoadSurveyIntervals
            .Where(i => meterIds.Contains(i.MeterId) && i.Quality != MeasurementQuality.NegativeConsumption)
            .GroupBy(i => i.MeterId)
            .Select(g => new { MeterId = g.Key, Last = g.OrderByDescending(i => i.IntervalEndUtc).First() })
            .ToDictionaryAsync(x => x.MeterId, x => x.Last.CumulativeReading, cancellationToken);

        var seenInBatch = new HashSet<(Guid MeterId, DateTime Start, DateTime End)>();

        // Cached per meter for the duration of this batch — thresholds don't change mid-request,
        // so there is no need to re-resolve the effective one for every interval of the same meter.
        var thresholdByMeter = new Dictionary<Guid, MeasurementRangeThreshold?>();

        foreach (var request in requests.OrderBy(r => r.IntervalStartUtc))
        {
            var key = (request.MeterId, request.IntervalStartUtc, request.IntervalEndUtc);
            if (!seenInBatch.Add(key))
            {
                // Duplicate within the same batch — do not persist it a second time (the row it
                // duplicates is already staged/saved), matching bug (1) fixed upstream.
                continue;
            }

            var alreadyPersisted = await _db.LoadSurveyIntervals.AnyAsync(
                i => i.MeterId == request.MeterId
                     && i.IntervalStartUtc == request.IntervalStartUtc
                     && i.IntervalEndUtc == request.IntervalEndUtc,
                cancellationToken);
            if (alreadyPersisted)
                continue;

            var hasPrior = lastReadingByMeter.TryGetValue(request.MeterId, out var priorReading);

            if (hasPrior && request.CumulativeReading < priorReading)
            {
                var rejected = LoadSurveyInterval.CreateRejected(
                    request.MeterId, request.IntervalStartUtc, request.IntervalEndUtc,
                    request.CumulativeReading, MeasurementQuality.NegativeConsumption);

                await RaiseOrReactivateHoldAsync(request.MeterId,
                    $"Negative consumption detected at interval {request.IntervalStartUtc:O}-{request.IntervalEndUtc:O} " +
                    $"(reading {request.CumulativeReading} < prior {priorReading}).", cancellationToken);

                _db.LoadSurveyIntervals.Add(rejected);
                results.Add(new LoadSurveyIngestResult(
                    request.MeterId, request.IntervalStartUtc, request.IntervalEndUtc,
                    rejected.Quality, rejected.Id));
                continue;
            }

            var consumption = hasPrior ? request.CumulativeReading - priorReading : 0m;
            var valid = LoadSurveyInterval.CreateValid(
                request.MeterId, request.IntervalStartUtc, request.IntervalEndUtc,
                request.CumulativeReading, consumption, MeasurementSource.Received);

            if (!thresholdByMeter.TryGetValue(request.MeterId, out var threshold))
            {
                threshold = await _outOfRangeValidationService.GetEffectiveThresholdAsync(
                    request.MeterId, MeasurementRangeType.LoadSurveyInterval, cancellationToken);
                thresholdByMeter[request.MeterId] = threshold;
            }

            // Out-of-range is a plausibility flag, not a sequence break: the cumulative reading
            // itself is trusted and still advances the sequence for the next interval — unlike a
            // negative-consumption rejection, it never raises a DataQualityHold on its own.
            if (threshold is not null && !threshold.IsWithinRange(consumption))
                valid.FlagOutOfRange();

            _db.LoadSurveyIntervals.Add(valid);
            lastReadingByMeter[request.MeterId] = request.CumulativeReading;

            results.Add(new LoadSurveyIngestResult(
                request.MeterId, request.IntervalStartUtc, request.IntervalEndUtc,
                valid.Quality, valid.Id));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return results;
    }

    private async Task RaiseOrReactivateHoldAsync(Guid meterId, string reason, CancellationToken cancellationToken)
    {
        var existing = await _db.DataQualityHolds
            .Where(h => h.MeterId == meterId)
            .OrderByDescending(h => h.RaisedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is { IsActive: true })
            return; // already blocking this meter; no need to duplicate.

        if (existing is not null)
        {
            existing.Reactivate(reason);
        }
        else
        {
            _db.DataQualityHolds.Add(DataQualityHold.Raise(meterId, reason));
        }
    }
}
