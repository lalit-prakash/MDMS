using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class ComplaintTests
{
    private static Complaint CreateComplaint(TimeSpan? sla = null) =>
        new(Guid.NewGuid(), null, ComplaintSource.Helpline1912, "No power since morning", sla ?? TimeSpan.FromHours(48));

    [Fact]
    public void Constructor_SetsOpenStatusAndSlaDueTime()
    {
        var complaint = CreateComplaint(TimeSpan.FromHours(24));

        Assert.Equal(ComplaintStatus.Open, complaint.Status);
        Assert.Equal(complaint.CreatedAtUtc.AddHours(24), complaint.SlaDueUtc);
    }

    [Fact]
    public void Constructor_BlankDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Complaint(Guid.NewGuid(), null, ComplaintSource.ConsumerPortal, "  ", TimeSpan.FromHours(48)));
    }

    [Fact]
    public void Constructor_NonPositiveSla_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Complaint(Guid.NewGuid(), null, ComplaintSource.ConsumerPortal, "desc", TimeSpan.Zero));
    }

    [Fact]
    public void AssignTo_FromOpen_MovesToAssigned()
    {
        var complaint = CreateComplaint();
        var userId = Guid.NewGuid();

        complaint.AssignTo(userId);

        Assert.Equal(ComplaintStatus.Assigned, complaint.Status);
        Assert.Equal(userId, complaint.AssignedToUserId);
    }

    [Fact]
    public void AssignTo_AlreadyResolved_Throws()
    {
        var complaint = CreateComplaint();
        complaint.AssignTo(Guid.NewGuid());
        complaint.StartProgress();
        complaint.Resolve("Fixed the fuse.");

        Assert.Throws<InvalidOperationException>(() => complaint.AssignTo(Guid.NewGuid()));
    }

    [Fact]
    public void StartProgress_WithoutAssignment_Throws()
    {
        var complaint = CreateComplaint();

        Assert.Throws<InvalidOperationException>(() => complaint.StartProgress());
    }

    [Fact]
    public void Close_WithoutResolving_Throws()
    {
        var complaint = CreateComplaint();
        complaint.AssignTo(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => complaint.Close());
    }

    [Fact]
    public void FullLifecycle_OpenToClosed_Succeeds()
    {
        var complaint = CreateComplaint();

        complaint.AssignTo(Guid.NewGuid());
        complaint.StartProgress();
        complaint.Resolve("Replaced the meter fuse.");
        complaint.Close();

        Assert.Equal(ComplaintStatus.Closed, complaint.Status);
        Assert.NotNull(complaint.ResolvedAtUtc);
        Assert.NotNull(complaint.ClosedAtUtc);
    }

    [Fact]
    public void IsOverdue_PastSlaAndNotClosed_ReturnsTrue()
    {
        var complaint = CreateComplaint(TimeSpan.FromHours(1));

        var isOverdue = complaint.IsOverdue(complaint.CreatedAtUtc.AddHours(2));

        Assert.True(isOverdue);
    }

    [Fact]
    public void IsOverdue_PastSlaButClosed_ReturnsFalse()
    {
        var complaint = CreateComplaint(TimeSpan.FromHours(1));
        complaint.AssignTo(Guid.NewGuid());
        complaint.StartProgress();
        complaint.Resolve("Done.");
        complaint.Close();

        var isOverdue = complaint.IsOverdue(complaint.CreatedAtUtc.AddHours(2));

        Assert.False(isOverdue);
    }
}
