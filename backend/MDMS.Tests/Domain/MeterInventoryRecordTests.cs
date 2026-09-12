using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class MeterInventoryRecordTests
{
    [Fact]
    public void CreateReceived_StartsAtReceived()
    {
        var record = MeterInventoryRecord.CreateReceived(Guid.NewGuid());

        Assert.Equal(MeterInventoryStatus.Received, record.Status);
    }

    [Fact]
    public void FullHappyPath_ReachesActive()
    {
        var record = MeterInventoryRecord.CreateReceived(Guid.NewGuid());
        var contractorId = Guid.NewGuid();
        var installerId = Guid.NewGuid();

        record.MoveToStore();
        record.AssignToContractor(contractorId);
        record.AssignToInstaller(installerId);
        record.MarkInstalled();
        record.Commission();
        record.Activate();

        Assert.Equal(MeterInventoryStatus.Active, record.Status);
        Assert.Equal(contractorId, record.ContractorUserId);
        Assert.Equal(installerId, record.InstallerUserId);
    }

    [Fact]
    public void AssignToContractor_SkippingMoveToStore_Throws()
    {
        var record = MeterInventoryRecord.CreateReceived(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => record.AssignToContractor(Guid.NewGuid()));
    }

    [Fact]
    public void ReplacementFlow_ActiveToRemoved_Succeeds()
    {
        var record = MeterInventoryRecord.CreateReceived(Guid.NewGuid());
        record.MoveToStore();
        record.AssignToContractor(Guid.NewGuid());
        record.AssignToInstaller(Guid.NewGuid());
        record.MarkInstalled();
        record.Commission();
        record.Activate();

        record.RequestReplacement("Meter malfunction reported by consumer.");
        record.MarkRemoved();

        Assert.Equal(MeterInventoryStatus.Removed, record.Status);
        Assert.Equal("Meter malfunction reported by consumer.", record.ReplacementReason);
    }

    [Fact]
    public void RequestReplacement_BlankReason_Throws()
    {
        var record = MeterInventoryRecord.CreateReceived(Guid.NewGuid());
        record.MoveToStore();
        record.AssignToContractor(Guid.NewGuid());
        record.AssignToInstaller(Guid.NewGuid());
        record.MarkInstalled();
        record.Commission();
        record.Activate();

        Assert.Throws<ArgumentException>(() => record.RequestReplacement("  "));
    }

    [Fact]
    public void RequestReplacement_NotActive_Throws()
    {
        var record = MeterInventoryRecord.CreateReceived(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => record.RequestReplacement("reason"));
    }
}
