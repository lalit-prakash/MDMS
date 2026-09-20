using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Application.Common;

/// <summary>A resolved Region→Section office chain; any level may be absent (a hierarchy need not
/// use every level).</summary>
public record OrgUnitChain(OrgUnit? Region, OrgUnit? Zone, OrgUnit? Circle, OrgUnit? Division, OrgUnit? SubDivision, OrgUnit? Section)
{
    public static readonly OrgUnitChain Empty = new(null, null, null, null, null, null);

    /// <summary>True if <paramref name="orgUnitId"/> is one of the units in this chain — i.e. this
    /// chain sits at or beneath that unit. Compares by id at whichever level the unit is, so
    /// selecting a Zone never accidentally matches on an unrelated null Circle.</summary>
    public bool Contains(Guid orgUnitId) =>
        Region?.Id == orgUnitId || Zone?.Id == orgUnitId || Circle?.Id == orgUnitId ||
        Division?.Id == orgUnitId || SubDivision?.Id == orgUnitId || Section?.Id == orgUnitId;
}

/// <summary>The one place that walks an OrgUnit up to its Region→Section ancestors, shared by every
/// endpoint that resolves the Substation's office (Network, Customers master, Meter Data filters).</summary>
public static class OrgUnitChainResolver
{
    public static Func<Guid?, OrgUnitChain> Build(IEnumerable<OrgUnit> allUnits)
    {
        var byId = allUnits.ToDictionary(u => u.Id);
        return id =>
        {
            OrgUnit? region = null, zone = null, circle = null, division = null, sub = null, section = null;
            var current = id.HasValue && byId.TryGetValue(id.Value, out var start) ? start : null;
            while (current is not null)
            {
                switch (current.UnitType)
                {
                    case OrgUnitType.Region: region = current; break;
                    case OrgUnitType.Zone: zone = current; break;
                    case OrgUnitType.Circle: circle = current; break;
                    case OrgUnitType.Division: division = current; break;
                    case OrgUnitType.SubDivision: sub = current; break;
                    case OrgUnitType.Section: section = current; break;
                }
                current = current.ParentId.HasValue && byId.TryGetValue(current.ParentId.Value, out var p) ? p : null;
            }
            return new OrgUnitChain(region, zone, circle, division, sub, section);
        };
    }
}
