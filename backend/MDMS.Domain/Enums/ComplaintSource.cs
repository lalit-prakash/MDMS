namespace MDMS.Domain.Enums;

/// <summary>Where a complaint originated — the spec's three stated channels.</summary>
public enum ComplaintSource
{
    ConsumerPortal = 1,
    MobileApp = 2,

    /// <summary>The consumer complaint helpline (referred to as "1912" in the source requirements).</summary>
    Helpline1912 = 3
}
