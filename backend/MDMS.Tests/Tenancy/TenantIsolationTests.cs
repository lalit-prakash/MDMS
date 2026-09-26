using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MDMS.Tests.Tenancy;

public class TenantIsolationTests
{
    private sealed class FixedTenant(Guid? id) : ITenantContext
    {
        public Guid? TenantId { get; } = id;
    }

    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static MdmsDbContext NewContext(string dbName, Guid? tenant) =>
        new(new DbContextOptionsBuilder<MdmsDbContext>().UseInMemoryDatabase(dbName).Options, new FixedTenant(tenant));

    [Fact]
    public async Task InsertedRows_AreStampedWithActiveTenant_AndOnlyVisibleToIt()
    {
        var db = Guid.NewGuid().ToString();
        await using (var a = NewContext(db, TenantA))
        {
            a.Customers.Add(new Customer("C-1", "Alpha"));
            await a.SaveChangesAsync();
        }
        await using (var b = NewContext(db, TenantB))
        {
            b.Customers.Add(new Customer("C-1", "Bravo"));
            await b.SaveChangesAsync();
        }

        await using var readA = NewContext(db, TenantA);
        var seenByA = await readA.Customers.ToListAsync();
        Assert.Single(seenByA);
        Assert.Equal("Alpha", seenByA[0].Name);
        Assert.Equal(TenantA, seenByA[0].TenantId);

        await using var readB = NewContext(db, TenantB);
        Assert.Equal("Bravo", Assert.Single(await readB.Customers.ToListAsync()).Name);
    }

    [Fact]
    public async Task NoActiveTenant_SeesNothing_UnlessFilterIsExplicitlyIgnored()
    {
        var db = Guid.NewGuid().ToString();
        await using (var a = NewContext(db, TenantA))
        {
            a.Customers.Add(new Customer("C-1", "Alpha"));
            await a.SaveChangesAsync();
        }

        await using var anonymous = NewContext(db, null);
        Assert.Empty(await anonymous.Customers.ToListAsync());
        Assert.Single(await anonymous.Customers.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task TenantRegistry_IsNotTenantScoped()
    {
        var db = Guid.NewGuid().ToString();
        await using (var a = NewContext(db, TenantA))
        {
            a.Tenants.Add(new Tenant("ORG-X", "Org X"));
            await a.SaveChangesAsync();
        }
        await using var b = NewContext(db, TenantB);
        Assert.Single(await b.Tenants.ToListAsync());
    }
}
