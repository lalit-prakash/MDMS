namespace MDMS.Domain.Enums;

/// <summary>
/// Event/alarm types a smart meter reports. "Alarm" isn't a separate table here — it's simply an
/// event whose <see cref="MeterEventSeverity"/> is Warning or Critical; modeling both in one
/// MeterEvent type (rather than duplicating a near-identical schema) matches how DLMS-based AMI
/// systems structure their own event/alarm profile classes.
/// </summary>
public enum MeterEventType
{
    PowerFailure,
    PowerRestore,
    TamperDetected,
    MagneticInfluence,
    CoverOpen,
    NeutralDisturbance,
    ClockChange,
    FirmwareUpdate,
    BatteryLow,
    OverVoltage,
    UnderVoltage,
    OverCurrent,
    LoadLimitBreach,
}
