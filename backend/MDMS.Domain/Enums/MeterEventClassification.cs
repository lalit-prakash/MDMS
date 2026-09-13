namespace MDMS.Domain.Enums;

/// <summary>The grouping every MeterEventType belongs to, for the classification-summary view
/// (KPI cards + summary table before drilling into individual occurrences).</summary>
public enum MeterEventClassification
{
    Voltage,
    Power,
    Security,
    Control,
    Communication,
    Other,
}

public static class MeterEventClassifier
{
    public static MeterEventClassification Classify(MeterEventType type) => type switch
    {
        MeterEventType.OverVoltage or MeterEventType.UnderVoltage => MeterEventClassification.Voltage,
        MeterEventType.PowerFailure or MeterEventType.PowerRestore or MeterEventType.OverCurrent => MeterEventClassification.Power,
        MeterEventType.TamperDetected or MeterEventType.MagneticInfluence or MeterEventType.CoverOpen or MeterEventType.NeutralDisturbance => MeterEventClassification.Security,
        MeterEventType.LoadLimitBreach => MeterEventClassification.Control,
        MeterEventType.ClockChange or MeterEventType.FirmwareUpdate or MeterEventType.BatteryLow => MeterEventClassification.Other,
        _ => MeterEventClassification.Other,
    };
}
