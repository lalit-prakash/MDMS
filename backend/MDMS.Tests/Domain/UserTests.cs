using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Constructor_AdminWithNoOrgUnit_Succeeds()
    {
        var admin = new User("admin", "Admin User", UserRole.Admin, orgUnitId: null);

        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.Null(admin.OrgUnitId);
    }

    [Fact]
    public void Constructor_NonAdminWithNoOrgUnit_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new User("supervisor1", "Supervisor One", UserRole.Supervisor, orgUnitId: null));
    }

    [Fact]
    public void Constructor_NonAdminWithOrgUnit_Succeeds()
    {
        var orgUnitId = Guid.NewGuid();
        var user = new User("installer1", "Installer One", UserRole.Installer, orgUnitId);

        Assert.Equal(orgUnitId, user.OrgUnitId);
    }

    [Fact]
    public void Reassign_ToNonAdminWithoutOrgUnit_Throws()
    {
        var user = new User("admin", "Admin User", UserRole.Admin, orgUnitId: null);

        Assert.Throws<ArgumentException>(() => user.Reassign(UserRole.Supervisor, orgUnitId: null));
    }

    [Fact]
    public void Reassign_ValidChange_Updates()
    {
        var orgUnitId = Guid.NewGuid();
        var user = new User("user1", "User One", UserRole.Installer, Guid.NewGuid());

        user.Reassign(UserRole.OmSupervisor, orgUnitId);

        Assert.Equal(UserRole.OmSupervisor, user.Role);
        Assert.Equal(orgUnitId, user.OrgUnitId);
    }
}
