using MDMS.Application.RevenueProtection;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Tests.RevenueProtection;

public class RevenueProtectionServiceTests
{
    private static MdmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MdmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MdmsDbContext(options);
    }

    [Fact]
    public async Task RaiseSignalAsync_NoExistingLead_CreatesOne()
    {
        await using var db = CreateContext();
        var service = new RevenueProtectionService(db);
        var customerId = Guid.NewGuid();

        await service.RaiseSignalAsync(customerId, null, RiskSignalType.TamperEvent, 20m, "cover open");

        var lead = await db.RevenueProtectionLeads.SingleAsync();
        Assert.Equal(customerId, lead.CustomerId);
        Assert.Equal(20m, lead.RiskScore);
    }

    [Fact]
    public async Task RaiseSignalAsync_ExistingOpenLead_AddsToSameLead()
    {
        await using var db = CreateContext();
        var service = new RevenueProtectionService(db);
        var customerId = Guid.NewGuid();

        await service.RaiseSignalAsync(customerId, null, RiskSignalType.TamperEvent, 20m, null);
        await service.RaiseSignalAsync(customerId, null, RiskSignalType.ConsumptionDeviation, 15m, null);

        var lead = await db.RevenueProtectionLeads.SingleAsync();
        Assert.Equal(35m, lead.RiskScore);
        Assert.Equal(2, await db.RiskSignals.CountAsync());
    }

    [Fact]
    public async Task RaiseSignalAsync_ExistingClosedLead_CreatesNewOne()
    {
        await using var db = CreateContext();
        var customerId = Guid.NewGuid();
        var closedLead = RevenueProtectionLead.Detect(customerId, null);
        closedLead.AddScore(10m);
        closedLead.Review();
        closedLead.AssignTo(Guid.NewGuid());
        closedLead.StartFieldInvestigation();
        closedLead.RecordFinding("finding");
        closedLead.RecordAction("action");
        closedLead.Close("closed");
        db.RevenueProtectionLeads.Add(closedLead);
        await db.SaveChangesAsync();

        var service = new RevenueProtectionService(db);
        await service.RaiseSignalAsync(customerId, null, RiskSignalType.TamperEvent, 20m, null);

        Assert.Equal(2, await db.RevenueProtectionLeads.CountAsync());
    }
}
