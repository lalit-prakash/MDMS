using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// One observed risk indicator contributing to a <see cref="RevenueProtectionLead"/>'s score.
/// The weight is supplied by the caller rather than a hard-coded table in code — per the spec's
/// own caution that "the actual weights must be calibrated by the utility and should not be
/// presented as legal proof", this project has no opinion on what a given signal type is worth.
/// </summary>
public class RiskSignal : Entity
{
    public Guid LeadId { get; private set; }
    public RiskSignalType SignalType { get; private set; }
    public decimal Weight { get; private set; }
    public string? Note { get; private set; }

    private RiskSignal() { }

    public static RiskSignal Raise(Guid leadId, RiskSignalType signalType, decimal weight, string? note)
    {
        if (weight < 0)
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight cannot be negative.");

        return new RiskSignal { LeadId = leadId, SignalType = signalType, Weight = weight, Note = note };
    }
}
