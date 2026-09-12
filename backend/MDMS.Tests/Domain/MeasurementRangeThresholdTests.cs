using MDMS.Domain.Entities;

namespace MDMS.Tests.Domain;

public class MeasurementRangeThresholdTests
{
    [Fact]
    public void Constructor_MaxNotGreaterThanMin_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MeasurementRangeThreshold(null, 10m, 10m));
        Assert.Throws<ArgumentException>(() => new MeasurementRangeThreshold(null, 10m, 5m));
    }

    [Fact]
    public void Constructor_NegativeMin_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeasurementRangeThreshold(null, -1m, 10m));
    }

    [Fact]
    public void IsWithinRange_BoundaryValuesAreInclusive()
    {
        var threshold = new MeasurementRangeThreshold(null, 0m, 100m);

        Assert.True(threshold.IsWithinRange(0m));
        Assert.True(threshold.IsWithinRange(100m));
        Assert.False(threshold.IsWithinRange(100.01m));
        Assert.False(threshold.IsWithinRange(-0.01m));
    }
}
