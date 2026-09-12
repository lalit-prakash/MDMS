using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Tests.Domain;

/// <summary>
/// Confirms the table-per-hierarchy generalization (VeeRuleDefinition base,
/// MeasurementRangeThreshold subclass) round-trips through EF Core correctly — the existing
/// MeasurementRangeThreshold-typed queries used throughout OutOfRangeValidationService and its
/// tests must keep seeing exactly the same rows as before this refactor.
/// </summary>
public class VeeRuleDefinitionPersistenceTests
{
    private static MdmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MdmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MdmsDbContext(options);
    }

    [Fact]
    public async Task MeasurementRangeThreshold_RoundTripsThroughVeeRuleDefinitionBase()
    {
        await using var db = CreateContext();
        var threshold = new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, null, 0m, 50m);
        db.MeasurementRangeThresholds.Add(threshold);
        await db.SaveChangesAsync();

        var viaBaseSet = await db.VeeRuleDefinitions.SingleAsync();
        Assert.IsType<MeasurementRangeThreshold>(viaBaseSet);
        Assert.Equal(VeeRuleType.OutOfRange, viaBaseSet.RuleType);
        Assert.True(viaBaseSet.IsActive);

        var viaDerivedSet = await db.MeasurementRangeThresholds.SingleAsync();
        Assert.Equal(50m, viaDerivedSet.MaxConsumptionKwh);
    }

    [Fact]
    public async Task DeactivatedThreshold_IsNoLongerTheEffectiveOne()
    {
        await using var db = CreateContext();
        var threshold = new MeasurementRangeThreshold(MeasurementRangeType.LoadSurveyInterval, null, 0m, 50m);
        db.MeasurementRangeThresholds.Add(threshold);
        await db.SaveChangesAsync();

        threshold.Deactivate();
        await db.SaveChangesAsync();

        var service = new Application.MeterData.OutOfRangeValidationService(db);
        var effective = await service.GetEffectiveThresholdAsync(Guid.NewGuid(), MeasurementRangeType.LoadSurveyInterval);

        Assert.Null(effective);
    }
}
