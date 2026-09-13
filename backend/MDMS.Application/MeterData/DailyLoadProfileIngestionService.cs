using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Application.MeterData;

/// <summary>
/// Validates and stores an incoming Daily Load Profile: a provisional/estimated profile created
/// because the real DLP hadn't arrived yet is swapped out the moment the real one does; a profile
/// already marked <see cref="MeasurementSource.Received"/> is never overwritten, since that would
/// silently discard a genuine meter-reported value. Also applies
/// <see cref="OutOfRangeValidationService"/>'s plausibility check inline, mirroring
/// <see cref="LoadSurveyIngestionService"/>'s own inline out-of-range flagging.
/// </summary>
public class DailyLoadProfileIngestionService
{
    private readonly IMdmsDbContext _db;
    private readonly OutOfRangeValidationService _outOfRangeValidationService;

    public DailyLoadProfileIngestionService(IMdmsDbContext db, OutOfRangeValidationService outOfRangeValidationService)
    {
        _db = db;
        _outOfRangeValidationService = outOfRangeValidationService;
    }

    public async Task<DailyLoadProfileIngestResult> IngestAsync(
        DailyLoadProfileIngestRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _db.DailyLoadProfiles.FirstOrDefaultAsync(
            p => p.ServicePointId == request.ServicePointId
                 && p.MeterId == request.MeterId
                 && p.ProfileDate == request.ProfileDate,
            cancellationToken);

        if (existing is { Source: MeasurementSource.Received })
        {
            throw new InvalidOperationException(
                $"A received Daily Load Profile already exists for meter {request.MeterId} on " +
                $"{request.ProfileDate:O} and is never overwritten.");
        }

        var replacedProvisional = existing is not null;
        if (existing is not null)
        {
            // Swap out the provisional row rather than mutating it in place — the entity itself
            // has no in-place "become Received" mutator (ReplaceWithReceived returns a new
            // instance) so identity/provenance of the provisional attempt is never blurred with
            // the real one.
            _db.DailyLoadProfiles.Remove(existing);
        }

        var received = DailyLoadProfile.CreateReceived(
            request.ServicePointId, request.MeterId, request.ProfileDate, request.ConsumptionKwh,
            request.KvahImport, request.KwhExport, request.KvahExport);

        var threshold = await _outOfRangeValidationService.GetEffectiveThresholdAsync(
            request.MeterId, MeasurementRangeType.DailyLoadProfile, cancellationToken);
        if (threshold is not null && !threshold.IsWithinRange(received.ConsumptionKwh))
            received.FlagOutOfRange();

        _db.DailyLoadProfiles.Add(received);
        await _db.SaveChangesAsync(cancellationToken);

        return new DailyLoadProfileIngestResult(
            received.Id, received.ServicePointId, received.MeterId, received.ProfileDate,
            received.ConsumptionKwh, received.Source, received.Quality, replacedProvisional);
    }
}
