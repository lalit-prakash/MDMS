using MDMS.Application.EnergyAudit;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Tests.EnergyAudit;

public class EnergyAuditServiceTests
{
    private static MdmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MdmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MdmsDbContext(options);
    }

    private static (HierarchyNode substation, HierarchyNode feeder, HierarchyNode dt) BuildHierarchy(MdmsDbContext db)
    {
        var substation = HierarchyNode.CreateSubstation("S1", "Substation One");
        var feeder = HierarchyNode.CreateChild(HierarchyNodeType.Feeder, substation, "F1", "Feeder One");
        var dt = HierarchyNode.CreateChild(HierarchyNodeType.DistributionTransformer, feeder, "DT1", "DT One");
        db.HierarchyNodes.AddRange(substation, feeder, dt);
        return (substation, feeder, dt);
    }

    [Fact]
    public async Task GetDescendantDistributionTransformerIdsAsync_FromSubstation_FindsDtThroughFeeder()
    {
        await using var db = CreateContext();
        var (substation, _, dt) = BuildHierarchy(db);
        await db.SaveChangesAsync();

        var service = new EnergyAuditService(db);
        var dtIds = await service.GetDescendantDistributionTransformerIdsAsync(substation.Id);

        Assert.Equal(new[] { dt.Id }, dtIds);
    }

    [Fact]
    public async Task GetDescendantDistributionTransformerIdsAsync_FromDtItself_ReturnsItself()
    {
        await using var db = CreateContext();
        var (_, _, dt) = BuildHierarchy(db);
        await db.SaveChangesAsync();

        var service = new EnergyAuditService(db);
        var dtIds = await service.GetDescendantDistributionTransformerIdsAsync(dt.Id);

        Assert.Equal(new[] { dt.Id }, dtIds);
    }

    [Fact]
    public async Task ComputeDailyBalanceAsync_NoInputReading_ReturnsNullInputAndDiscrepancy()
    {
        await using var db = CreateContext();
        var (substation, _, _) = BuildHierarchy(db);
        await db.SaveChangesAsync();

        var service = new EnergyAuditService(db);
        var result = await service.ComputeDailyBalanceAsync(substation.Id, new DateOnly(2026, 1, 1));

        Assert.Null(result.InputEnergyKwh);
        Assert.Null(result.DiscrepancyKwh);
        Assert.Equal(0m, result.AccountedEnergyKwh);
    }

    [Fact]
    public async Task ComputeDailyBalanceAsync_SumsDownstreamConsumptionAndComputesDiscrepancy()
    {
        await using var db = CreateContext();
        var (substation, _, dt) = BuildHierarchy(db);

        var customer = new Customer("ACC-1", "Test Consumer");
        var servicePoint = new ServicePoint(customer.Id, "1 Main St");
        servicePoint.AssignDistributionTransformer(dt);
        db.Customers.Add(customer);
        db.ServicePoints.Add(servicePoint);

        var date = new DateOnly(2026, 1, 1);
        db.DailyLoadProfiles.Add(DailyLoadProfile.CreateReceived(servicePoint.Id, Guid.NewGuid(), date, 30m));
        db.NetworkEnergyReadings.Add(new NetworkEnergyReading(substation.Id, date, 100m));
        await db.SaveChangesAsync();

        var service = new EnergyAuditService(db);
        var result = await service.ComputeDailyBalanceAsync(substation.Id, date);

        Assert.Equal(100m, result.InputEnergyKwh);
        Assert.Equal(30m, result.AccountedEnergyKwh);
        Assert.Equal(70m, result.DiscrepancyKwh);
        Assert.Equal(70m, result.DiscrepancyPercent);
        Assert.Equal(1, result.LinkedServicePointCount);
        Assert.Equal(1, result.ServicePointsWithDataCount);
        Assert.Equal(100m, result.DataCompletenessPercent);
    }

    [Fact]
    public async Task ComputeDailyBalanceAsync_ServicePointWithNoProfileForDate_ReducesCompleteness()
    {
        await using var db = CreateContext();
        var (substation, _, dt) = BuildHierarchy(db);

        var customer1 = new Customer("ACC-1", "Consumer One");
        var sp1 = new ServicePoint(customer1.Id, "1 Main St");
        sp1.AssignDistributionTransformer(dt);

        var customer2 = new Customer("ACC-2", "Consumer Two");
        var sp2 = new ServicePoint(customer2.Id, "2 Main St");
        sp2.AssignDistributionTransformer(dt);

        db.Customers.AddRange(customer1, customer2);
        db.ServicePoints.AddRange(sp1, sp2);

        var date = new DateOnly(2026, 1, 1);
        db.DailyLoadProfiles.Add(DailyLoadProfile.CreateReceived(sp1.Id, Guid.NewGuid(), date, 30m));
        // sp2 has no profile for this date at all.
        await db.SaveChangesAsync();

        var service = new EnergyAuditService(db);
        var result = await service.ComputeDailyBalanceAsync(substation.Id, date);

        Assert.Equal(2, result.LinkedServicePointCount);
        Assert.Equal(1, result.ServicePointsWithDataCount);
        Assert.Equal(50m, result.DataCompletenessPercent);
    }
}
