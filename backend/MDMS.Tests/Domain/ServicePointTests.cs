using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class ServicePointTests
{
    [Fact]
    public void AssignDistributionTransformer_DtNode_Succeeds()
    {
        var servicePoint = new ServicePoint(Guid.NewGuid(), "1 Main St");
        var substation = HierarchyNode.CreateSubstation("S1", "Substation One");
        var feeder = HierarchyNode.CreateChild(HierarchyNodeType.Feeder, substation, "F1", "Feeder One");
        var dt = HierarchyNode.CreateChild(HierarchyNodeType.DistributionTransformer, feeder, "DT1", "DT One");

        servicePoint.AssignDistributionTransformer(dt);

        Assert.Equal(dt.Id, servicePoint.DistributionTransformerNodeId);
    }

    [Fact]
    public void AssignDistributionTransformer_NonDtNode_Throws()
    {
        var servicePoint = new ServicePoint(Guid.NewGuid(), "1 Main St");
        var substation = HierarchyNode.CreateSubstation("S1", "Substation One");

        Assert.Throws<ArgumentException>(() => servicePoint.AssignDistributionTransformer(substation));
    }
}
