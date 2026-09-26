using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Xunit;

namespace MDMS.Tests.Services;

public class MeterTestingRequestTests
{
    private static MeterTestingRequest NewRequest() =>
        new(Guid.NewGuid(), Guid.NewGuid(), MeterTestingReason.SuspectedIncorrectReading, "seems high");

    [Fact]
    public void NewRequest_IsSubmitted_WithARequestNumber()
    {
        var r = NewRequest();
        Assert.Equal(MeterTestingStatus.Submitted, r.Status);
        Assert.StartsWith("MTRT-", r.RequestNumber);
    }

    [Fact]
    public void Lifecycle_SubmittedToScheduledToCompleted()
    {
        var r = NewRequest();
        r.Schedule(DateTime.UtcNow.AddDays(2));
        Assert.Equal(MeterTestingStatus.Scheduled, r.Status);

        r.Complete("Meter Passed", "Within Permissible Limit", null);
        Assert.Equal(MeterTestingStatus.Completed, r.Status);
        Assert.NotNull(r.CompletedAtUtc);
    }

    [Fact]
    public void Complete_WithoutScheduling_Throws()
        => Assert.Throws<InvalidOperationException>(() => NewRequest().Complete("Passed", "OK", null));

    [Fact]
    public void Cancel_OnCompletedRequest_Throws()
    {
        var r = NewRequest();
        r.Schedule(DateTime.UtcNow);
        r.Complete("Passed", "OK", null);
        Assert.Throws<InvalidOperationException>(r.Cancel);
    }
}
