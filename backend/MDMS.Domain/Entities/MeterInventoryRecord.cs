using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// Tracks a physical meter's WFM/inventory allocation workflow — deliberately separate from
/// <see cref="Meter.Status"/> (that meter's own simple master-data lifecycle: InStock/Installed/
/// Removed/Retired). This carries the richer allocation state machine involving actors (store
/// manager, contractor, installer) that master-data status was never meant to represent. One
/// record per meter's current inventory journey.
/// </summary>
public class MeterInventoryRecord : Entity
{
    public Guid MeterId { get; private set; }
    public MeterInventoryStatus Status { get; private set; }
    public Guid? ContractorUserId { get; private set; }
    public Guid? InstallerUserId { get; private set; }
    public string? ReplacementReason { get; private set; }

    private MeterInventoryRecord() { }

    public static MeterInventoryRecord CreateReceived(Guid meterId)
        => new() { MeterId = meterId, Status = MeterInventoryStatus.Received };

    public void MoveToStore()
    {
        RequireStatus(MeterInventoryStatus.Received);
        Status = MeterInventoryStatus.InStore;
    }

    public void AssignToContractor(Guid contractorUserId)
    {
        RequireStatus(MeterInventoryStatus.InStore);
        ContractorUserId = contractorUserId;
        Status = MeterInventoryStatus.AssignedToContractor;
    }

    public void AssignToInstaller(Guid installerUserId)
    {
        RequireStatus(MeterInventoryStatus.AssignedToContractor);
        InstallerUserId = installerUserId;
        Status = MeterInventoryStatus.AssignedToInstaller;
    }

    public void MarkInstalled()
    {
        RequireStatus(MeterInventoryStatus.AssignedToInstaller);
        Status = MeterInventoryStatus.Installed;
    }

    public void Commission()
    {
        RequireStatus(MeterInventoryStatus.Installed);
        Status = MeterInventoryStatus.Commissioned;
    }

    public void Activate()
    {
        RequireStatus(MeterInventoryStatus.Commissioned);
        Status = MeterInventoryStatus.Active;
    }

    public void RequestReplacement(string reason)
    {
        RequireStatus(MeterInventoryStatus.Active);
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A replacement request requires a reason.", nameof(reason));

        ReplacementReason = reason;
        Status = MeterInventoryStatus.ReplacementRequested;
    }

    public void MarkRemoved()
    {
        RequireStatus(MeterInventoryStatus.ReplacementRequested);
        Status = MeterInventoryStatus.Removed;
    }

    private void RequireStatus(MeterInventoryStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Expected status {expected} but was {Status}.");
    }
}
