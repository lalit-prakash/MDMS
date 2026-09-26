using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Xunit;

namespace MDMS.Tests.Services;

public class ServiceRequestTests
{
    private static ServiceRequest NewRequest() =>
        new(Guid.NewGuid(), ServiceRequestCategory.MeterShifting, "Please shift meter to compound wall");

    [Fact]
    public void NewRequest_IsOpen_WithARequestNumber()
    {
        var r = NewRequest();
        Assert.Equal(ServiceRequestStatus.Open, r.Status);
        Assert.StartsWith("SR-", r.RequestNumber);
    }

    [Fact]
    public void Lifecycle_OpenToInProgressToCompleted()
    {
        var r = NewRequest();
        r.StartProgress();
        Assert.Equal(ServiceRequestStatus.InProgress, r.Status);

        r.Complete("Meter shifted as requested.");
        Assert.Equal(ServiceRequestStatus.Completed, r.Status);
        Assert.NotNull(r.CompletedAtUtc);
    }

    [Fact]
    public void Constructor_RequiresDescription()
        => Assert.Throws<ArgumentException>(() => new ServiceRequest(Guid.NewGuid(), ServiceRequestCategory.Other, " "));

    [Fact]
    public void Cancel_OnCompletedRequest_Throws()
    {
        var r = NewRequest();
        r.Complete("done");
        Assert.Throws<InvalidOperationException>(r.Cancel);
    }
}
