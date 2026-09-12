using MDMS.Domain.Entities;

namespace MDMS.Tests.Domain;

public class NetworkEnergyReadingTests
{
    [Fact]
    public void Constructor_NegativeEnergy_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new NetworkEnergyReading(Guid.NewGuid(), new DateOnly(2026, 1, 1), -1m));
    }

    [Fact]
    public void Revise_UpdatesEnergy()
    {
        var reading = new NetworkEnergyReading(Guid.NewGuid(), new DateOnly(2026, 1, 1), 100m);

        reading.Revise(120m);

        Assert.Equal(120m, reading.EnergyKwh);
    }

    [Fact]
    public void Revise_NegativeEnergy_Throws()
    {
        var reading = new NetworkEnergyReading(Guid.NewGuid(), new DateOnly(2026, 1, 1), 100m);

        Assert.Throws<ArgumentOutOfRangeException>(() => reading.Revise(-5m));
    }
}
