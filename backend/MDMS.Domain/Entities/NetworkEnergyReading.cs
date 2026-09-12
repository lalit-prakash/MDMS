using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>
/// The energy that entered one <see cref="HierarchyNode"/> (Substation/Feeder/DT) on one day —
/// the "supply side" figure energy-audit balance compares against downstream consumer
/// consumption. Sourced from a boundary meter at that network level; this project has no feeder/
/// DTR meter ingestion pipeline yet, so today a reading is entered directly (see
/// <c>EnergyAuditController</c>). One reading per node/day is authoritative — a later one
/// replaces rather than adds.
/// </summary>
public class NetworkEnergyReading : Entity
{
    public Guid HierarchyNodeId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal EnergyKwh { get; private set; }

    private NetworkEnergyReading() { }

    public NetworkEnergyReading(Guid hierarchyNodeId, DateOnly date, decimal energyKwh)
    {
        if (energyKwh < 0)
            throw new ArgumentOutOfRangeException(nameof(energyKwh), "Energy cannot be negative.");

        HierarchyNodeId = hierarchyNodeId;
        Date = date;
        EnergyKwh = energyKwh;
    }

    public void Revise(decimal energyKwh)
    {
        if (energyKwh < 0)
            throw new ArgumentOutOfRangeException(nameof(energyKwh), "Energy cannot be negative.");

        EnergyKwh = energyKwh;
    }
}
