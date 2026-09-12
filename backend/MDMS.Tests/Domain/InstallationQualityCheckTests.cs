using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class InstallationQualityCheckTests
{
    private static InstallationQualityCheck CreateAtL1Pending()
    {
        var check = InstallationQualityCheck.Create(Guid.NewGuid());
        check.StartL1();
        return check;
    }

    [Fact]
    public void Create_StartsAtDraft()
    {
        var check = InstallationQualityCheck.Create(Guid.NewGuid());

        Assert.Equal(InstallationQualityStatus.Draft, check.Status);
    }

    [Fact]
    public void FullHappyPath_ReachesMisOnboarded()
    {
        var check = CreateAtL1Pending();
        var l1User = Guid.NewGuid();
        var l2User = Guid.NewGuid();
        var l3User = Guid.NewGuid();

        check.CompleteL1(l1User);
        check.SubmitToL2();
        check.ApproveL2(l2User);
        check.SubmitToL3();
        check.ApproveL3(l3User);
        check.MarkRmsSyncPending();
        check.MarkRmsSynced();
        check.MarkMisOnboarded();

        Assert.Equal(InstallationQualityStatus.MisOnboarded, check.Status);
        Assert.Equal(l1User, check.L1CompletedByUserId);
        Assert.Equal(l2User, check.L2DecisionByUserId);
        Assert.Equal(l3User, check.L3DecisionByUserId);
    }

    [Fact]
    public void RejectL2_ReturnsToL1Pending_WithNote()
    {
        var check = CreateAtL1Pending();
        check.CompleteL1(Guid.NewGuid());
        check.SubmitToL2();

        check.RejectL2(Guid.NewGuid(), "Wiring photo missing.");

        Assert.Equal(InstallationQualityStatus.L1Pending, check.Status);
        Assert.Equal("Wiring photo missing.", check.L2RejectionNote);
    }

    [Fact]
    public void RejectL2_BlankNote_Throws()
    {
        var check = CreateAtL1Pending();
        check.CompleteL1(Guid.NewGuid());
        check.SubmitToL2();

        Assert.Throws<ArgumentException>(() => check.RejectL2(Guid.NewGuid(), " "));
    }

    [Fact]
    public void RejectL3_ReturnsToL2Pending_NotAllTheWayToDraft()
    {
        var check = CreateAtL1Pending();
        check.CompleteL1(Guid.NewGuid());
        check.SubmitToL2();
        check.ApproveL2(Guid.NewGuid());
        check.SubmitToL3();

        check.RejectL3(Guid.NewGuid(), "Meter reading photo unclear.");

        Assert.Equal(InstallationQualityStatus.L2Pending, check.Status);
    }

    [Fact]
    public void ApproveL2_WhenNotL2Pending_Throws()
    {
        var check = InstallationQualityCheck.Create(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => check.ApproveL2(Guid.NewGuid()));
    }

    [Fact]
    public void MarkMisOnboarded_BeforeRmsSynced_Throws()
    {
        var check = InstallationQualityCheck.Create(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => check.MarkMisOnboarded());
    }
}
