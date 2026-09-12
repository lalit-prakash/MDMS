using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Application.RevenueProtection;

/// <summary>
/// Raises risk signals against a lead, creating one if none is currently open for the customer.
/// One open (non-Closed) lead per customer at a time — a second signal for the same customer
/// contributes to the existing investigation rather than fragmenting it into a new one.
/// </summary>
public class RevenueProtectionService
{
    private readonly IMdmsDbContext _db;

    public RevenueProtectionService(IMdmsDbContext db)
    {
        _db = db;
    }

    public async Task<RiskSignal> RaiseSignalAsync(
        Guid customerId, Guid? meterId, RiskSignalType signalType, decimal weight, string? note,
        CancellationToken cancellationToken = default)
    {
        var lead = await _db.RevenueProtectionLeads
            .Where(l => l.CustomerId == customerId && l.Status != RevenueProtectionLeadStatus.Closed)
            .OrderByDescending(l => l.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (lead is null)
        {
            lead = RevenueProtectionLead.Detect(customerId, meterId);
            _db.RevenueProtectionLeads.Add(lead);
        }

        var signal = RiskSignal.Raise(lead.Id, signalType, weight, note);
        lead.AddScore(weight);

        _db.RiskSignals.Add(signal);
        await _db.SaveChangesAsync(cancellationToken);

        return signal;
    }
}
