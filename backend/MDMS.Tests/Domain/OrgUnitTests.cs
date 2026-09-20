using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class OrgUnitTests
{
    [Fact]
    public void CreateZone_HasNoParent()
    {
        var zone = OrgUnit.CreateZone("Z1", "Zone One");

        Assert.Equal(OrgUnitType.Zone, zone.UnitType);
        Assert.Null(zone.ParentId);
    }

    [Fact]
    public void CreateChild_FullChain_Succeeds()
    {
        var zone = OrgUnit.CreateZone("Z1", "Zone One");
        var circle = OrgUnit.CreateChild(OrgUnitType.Circle, zone, "C1", "Circle One");
        var division = OrgUnit.CreateChild(OrgUnitType.Division, circle, "D1", "Division One");
        var subDivision = OrgUnit.CreateChild(OrgUnitType.SubDivision, division, "SD1", "Sub Division One");
        var section = OrgUnit.CreateChild(OrgUnitType.Section, subDivision, "SEC1", "Section One");

        Assert.Equal(zone.Id, circle.ParentId);
        Assert.Equal(circle.Id, division.ParentId);
        Assert.Equal(division.Id, subDivision.ParentId);
        Assert.Equal(subDivision.Id, section.ParentId);
    }

    [Fact]
    public void CreateChild_SkippingALevel_Throws()
    {
        var zone = OrgUnit.CreateZone("Z1", "Zone One");

        // A Division must nest under a Circle, never directly under a Zone.
        Assert.Throws<ArgumentException>(() =>
            OrgUnit.CreateChild(OrgUnitType.Division, zone, "D1", "Division One"));
    }

    [Fact]
    public void CreateChild_ZoneUnitType_Throws()
    {
        var zone = OrgUnit.CreateZone("Z1", "Zone One");

        Assert.Throws<ArgumentException>(() =>
            OrgUnit.CreateChild(OrgUnitType.Zone, zone, "Z2", "Zone Two"));
    }

    [Fact]
    public void Region_CanParentAZone_ButNotACircle()
    {
        var region = OrgUnit.CreateRegion("R1", "Region One");
        var zone = OrgUnit.CreateChild(OrgUnitType.Zone, region, "Z1", "Zone One");
        Assert.Equal(region.Id, zone.ParentId);
        Assert.Throws<ArgumentException>(() => OrgUnit.CreateChild(OrgUnitType.Circle, region, "C1", "Circle One"));
    }
}
