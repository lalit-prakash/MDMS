using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class VeeExecutionRecordTests
{
    [Fact]
    public void Record_BlankDetails_Throws()
    {
        Assert.Throws<ArgumentException>(() => VeeExecutionRecord.Record(
            "Rule", MeasurementRangeType.LoadSurveyInterval, Guid.NewGuid(),
            DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), MeasurementQuality.Valid, 5m, " "));
    }

    [Fact]
    public void Record_ValidInput_PopulatesFields()
    {
        var meterId = Guid.NewGuid();
        var start = DateTime.UtcNow;

        var record = VeeExecutionRecord.Record(
            "AverageOfSurroundingPeriods", MeasurementRangeType.LoadSurveyInterval, meterId,
            start, start.AddMinutes(30), MeasurementQuality.Valid, 5m, "estimated");

        Assert.Equal(meterId, record.MeterId);
        Assert.Equal(5m, record.NewValue);
        Assert.Equal(MeasurementQuality.Valid, record.ResultQuality);
    }
}
