using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// A physical meter asset — master data only (serial number, phase, status). Which service
/// point it is currently installed at is tracked via <see cref="MeterAssignment"/>, not a
/// direct property here, because that relationship is temporal (a meter can be replaced,
/// removed, and reassigned) and must stay auditable per MDMS's provenance principles.
/// </summary>
public class Meter : Entity
{
    public string SerialNumber { get; private set; } = default!;
    public MeterPhase Phase { get; private set; }
    public MeterStatus Status { get; private set; }

    private Meter() { }

    public Meter(string serialNumber, MeterPhase phase)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("Serial number is required.", nameof(serialNumber));

        SerialNumber = serialNumber;
        Phase = phase;
        Status = MeterStatus.InStock;
    }

    public void MarkInstalled() => Status = MeterStatus.Installed;

    public void MarkRemoved() => Status = MeterStatus.Removed;

    public void Retire() => Status = MeterStatus.Retired;
}
