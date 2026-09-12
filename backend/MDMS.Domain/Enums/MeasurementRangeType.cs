namespace MDMS.Domain.Enums;

/// <summary>Which measurement product a <see cref="Entities.MeasurementRangeThreshold"/> applies to.</summary>
public enum MeasurementRangeType
{
    /// <summary>A single 30-minute Load Survey interval's consumption.</summary>
    LoadSurveyInterval = 1,

    /// <summary>A full day's Daily Load Profile consumption.</summary>
    DailyLoadProfile = 2
}
