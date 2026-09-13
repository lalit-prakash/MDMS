namespace MDMS.Domain.Enums;

/// <summary>The meter's own load-limiting state at the moment an Instantaneous Profile was captured.</summary>
public enum LoadLimitState
{
    Normal,
    Limited,
    Disconnected,
}
