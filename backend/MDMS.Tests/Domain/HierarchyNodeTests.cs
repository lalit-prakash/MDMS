using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class HierarchyNodeTests
{
    [Fact]
    public void CreateSubstation_HasNoParent()
    {
        var substation = HierarchyNode.CreateSubstation("S1", "Substation One");

        Assert.Equal(HierarchyNodeType.Substation, substation.NodeType);
        Assert.Null(substation.ParentId);
    }

    [Fact]
    public void CreateChild_CorrectParentLevel_Succeeds()
    {
        var substation = HierarchyNode.CreateSubstation("S1", "Substation One");
        var feeder = HierarchyNode.CreateChild(HierarchyNodeType.Feeder, substation, "F1", "Feeder One");
        var dt = HierarchyNode.CreateChild(HierarchyNodeType.DistributionTransformer, feeder, "DT1", "DT One");

        Assert.Equal(substation.Id, feeder.ParentId);
        Assert.Equal(feeder.Id, dt.ParentId);
    }

    [Fact]
    public void CreateChild_WrongParentLevel_Throws()
    {
        var substation = HierarchyNode.CreateSubstation("S1", "Substation One");

        // A DT must nest under a Feeder, never directly under a Substation.
        Assert.Throws<ArgumentException>(() =>
            HierarchyNode.CreateChild(HierarchyNodeType.DistributionTransformer, substation, "DT1", "DT One"));
    }

    [Fact]
    public void CreateChild_SubstationNodeType_Throws()
    {
        var substation = HierarchyNode.CreateSubstation("S1", "Substation One");

        Assert.Throws<ArgumentException>(() =>
            HierarchyNode.CreateChild(HierarchyNodeType.Substation, substation, "S2", "Substation Two"));
    }
}
