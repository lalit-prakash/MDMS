using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Xunit;

namespace MDMS.Tests.Domain;

public class DownloadRequestTests
{
    private static DownloadRequest NewRequest() => new(Guid.NewGuid(), "Feeders", "/api/v1/network/feeders");

    [Fact]
    public void NewRequest_IsPending()
        => Assert.Equal(DownloadRequestStatus.Pending, NewRequest().Status);

    [Fact]
    public void Lifecycle_PendingToProcessingToCompleted_StoresFile()
    {
        var r = NewRequest();
        r.MarkProcessing();
        r.Complete("a.csv", new byte[] { 1, 2, 3 }, 2);
        Assert.Equal(DownloadRequestStatus.Completed, r.Status);
        Assert.Equal(3, r.SizeBytes);
        Assert.Equal(2, r.RowCount);
        Assert.NotNull(r.CompletedAtUtc);
    }

    [Fact]
    public void Complete_WithoutProcessing_Throws()
        => Assert.Throws<InvalidOperationException>(() => NewRequest().Complete("a.csv", new byte[1], 0));

    [Fact]
    public void Fail_RecordsMessage_AndCompletedRequestCannotFail()
    {
        var failed = NewRequest();
        failed.Fail("boom");
        Assert.Equal(DownloadRequestStatus.Failed, failed.Status);
        Assert.Equal("boom", failed.ErrorMessage);

        var done = NewRequest();
        done.MarkProcessing();
        done.Complete("a.csv", new byte[1], 0);
        Assert.Throws<InvalidOperationException>(() => done.Fail("late"));
    }

    [Fact]
    public void Constructor_RequiresTitleAndPath()
    {
        Assert.Throws<ArgumentException>(() => new DownloadRequest(Guid.NewGuid(), " ", "/api/v1/x"));
        Assert.Throws<ArgumentException>(() => new DownloadRequest(Guid.NewGuid(), "t", ""));
    }
}
