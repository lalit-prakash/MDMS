using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// An immutable audit entry for one VEE rule execution against one measurement slot — what rule
/// ran, what it found/did, and why. Every estimation and every out-of-range/negative-consumption
/// outcome should be explainable after the fact without re-deriving it; this is that record.
/// </summary>
public class VeeExecutionRecord : Entity
{
    public string RuleName { get; private set; } = default!;
    public MeasurementRangeType MeasurementType { get; private set; }
    public Guid MeterId { get; private set; }
    public DateTime SlotStartUtc { get; private set; }
    public DateTime SlotEndUtc { get; private set; }
    public MeasurementQuality ResultQuality { get; private set; }
    public decimal? NewValue { get; private set; }
    public string Details { get; private set; } = default!;

    private VeeExecutionRecord() { }

    public static VeeExecutionRecord Record(
        string ruleName, MeasurementRangeType measurementType, Guid meterId,
        DateTime slotStartUtc, DateTime slotEndUtc, MeasurementQuality resultQuality,
        decimal? newValue, string details)
    {
        if (string.IsNullOrWhiteSpace(ruleName))
            throw new ArgumentException("Rule name is required.", nameof(ruleName));
        if (string.IsNullOrWhiteSpace(details))
            throw new ArgumentException("Details are required — a VEE audit entry must explain its outcome.", nameof(details));

        return new VeeExecutionRecord
        {
            RuleName = ruleName,
            MeasurementType = measurementType,
            MeterId = meterId,
            SlotStartUtc = slotStartUtc,
            SlotEndUtc = slotEndUtc,
            ResultQuality = resultQuality,
            NewValue = newValue,
            Details = details
        };
    }
}
