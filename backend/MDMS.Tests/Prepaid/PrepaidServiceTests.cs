using MDMS.Application.Prepaid;
using MDMS.Domain.Entities;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Tests.Prepaid;

public class PrepaidServiceTests
{
    private static MdmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MdmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MdmsDbContext(options);
    }

    [Fact]
    public async Task RechargeAsync_NewCustomer_OpensAccountAndCredits()
    {
        await using var db = CreateContext();
        var service = new PrepaidService(db);
        var customerId = Guid.NewGuid();

        var transaction = await service.RechargeAsync(customerId, 300m, "PAY-1");

        Assert.Equal(300m, transaction.BalanceAfter);
        var account = await db.PrepaidAccounts.SingleAsync();
        Assert.Equal(customerId, account.CustomerId);
        Assert.Equal(300m, account.Balance);
    }

    [Fact]
    public async Task RechargeAsync_RepeatedReference_IsIdempotent()
    {
        await using var db = CreateContext();
        var service = new PrepaidService(db);
        var customerId = Guid.NewGuid();

        var first = await service.RechargeAsync(customerId, 300m, "PAY-1");
        var second = await service.RechargeAsync(customerId, 300m, "PAY-1");

        Assert.Equal(first.Id, second.Id);
        var account = await db.PrepaidAccounts.SingleAsync();
        Assert.Equal(300m, account.Balance); // not 600
        Assert.Equal(1, await db.WalletTransactions.CountAsync());
    }

    [Fact]
    public async Task ProcessDailyBillingAsync_NoConsumption_ReturnsNullAndDoesNotDebit()
    {
        await using var db = CreateContext();
        var service = new PrepaidService(db);
        var customerId = Guid.NewGuid();
        await service.RechargeAsync(customerId, 100m, "PAY-1");

        var result = await service.ProcessDailyBillingAsync(customerId, new DateOnly(2026, 1, 1), 8m);

        Assert.Null(result);
        var account = await db.PrepaidAccounts.SingleAsync();
        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public async Task ProcessDailyBillingAsync_WithConsumption_DebitsAtFlatRate()
    {
        await using var db = CreateContext();
        var customerId = Guid.NewGuid();
        var customer = new Customer("ACC-1", "Test Consumer");
        // Force the id to match via reflection isn't possible (private setter) - use the real Id.
        db.Customers.Add(customer);
        var servicePoint = new ServicePoint(customer.Id, "1 Main St");
        db.ServicePoints.Add(servicePoint);

        var date = new DateOnly(2026, 1, 1);
        db.DailyLoadProfiles.Add(DailyLoadProfile.CreateReceived(servicePoint.Id, Guid.NewGuid(), date, 20m));
        await db.SaveChangesAsync();

        var service = new PrepaidService(db);
        await service.RechargeAsync(customer.Id, 500m, "PAY-1");

        var transaction = await service.ProcessDailyBillingAsync(customer.Id, date, 8m);

        Assert.NotNull(transaction);
        Assert.Equal(-160m, transaction!.Amount); // 20 kWh * 8/kWh
        var account = await db.PrepaidAccounts.SingleAsync(a => a.CustomerId == customer.Id);
        Assert.Equal(340m, account.Balance); // 500 - 160
    }

    [Fact]
    public async Task ProcessDailyBillingAsync_RepeatedForSameDay_IsIdempotent()
    {
        await using var db = CreateContext();
        var customer = new Customer("ACC-1", "Test Consumer");
        db.Customers.Add(customer);
        var servicePoint = new ServicePoint(customer.Id, "1 Main St");
        db.ServicePoints.Add(servicePoint);
        var date = new DateOnly(2026, 1, 1);
        db.DailyLoadProfiles.Add(DailyLoadProfile.CreateReceived(servicePoint.Id, Guid.NewGuid(), date, 20m));
        await db.SaveChangesAsync();

        var service = new PrepaidService(db);
        await service.RechargeAsync(customer.Id, 500m, "PAY-1");

        await service.ProcessDailyBillingAsync(customer.Id, date, 8m);
        await service.ProcessDailyBillingAsync(customer.Id, date, 8m);

        var account = await db.PrepaidAccounts.SingleAsync(a => a.CustomerId == customer.Id);
        Assert.Equal(340m, account.Balance); // only debited once
    }
}
