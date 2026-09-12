using MDMS.Domain.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class TenantScopingTests
{
    [Fact]
    public void NewEntity_DefaultsToDefaultTenant()
    {
        var meter = new Meter("MTR-1", MeterPhase.Single);

        Assert.Equal(Entity.DefaultTenantId, meter.TenantId);
    }

    [Fact]
    public void AssignTenant_ChangesTenantId()
    {
        var meter = new Meter("MTR-1", MeterPhase.Single);
        var otherTenant = Guid.NewGuid();

        meter.AssignTenant(otherTenant);

        Assert.Equal(otherTenant, meter.TenantId);
    }
}
