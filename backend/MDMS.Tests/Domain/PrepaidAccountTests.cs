using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class PrepaidAccountTests
{
    [Fact]
    public void Open_StartsAtZeroBalanceAndConnected()
    {
        var account = PrepaidAccount.Open(Guid.NewGuid());

        Assert.Equal(0m, account.Balance);
        Assert.True(account.IsConnected);
    }

    [Fact]
    public void Recharge_IncreasesBalance_AndReturnsTransaction()
    {
        var account = PrepaidAccount.Open(Guid.NewGuid());

        var transaction = account.Recharge(500m, "PAY-1");

        Assert.Equal(500m, account.Balance);
        Assert.Equal(WalletTransactionType.Recharge, transaction.Type);
        Assert.Equal(500m, transaction.Amount);
        Assert.Equal(500m, transaction.BalanceAfter);
    }

    [Fact]
    public void Recharge_NonPositiveAmount_Throws()
    {
        var account = PrepaidAccount.Open(Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Recharge(0m, "PAY-1"));
        Assert.Throws<ArgumentOutOfRangeException>(() => account.Recharge(-10m, "PAY-2"));
    }

    [Fact]
    public void DebitForConsumption_DecreasesBalance_CanGoNegative()
    {
        var account = PrepaidAccount.Open(Guid.NewGuid());
        account.Recharge(100m, "PAY-1");

        var transaction = account.DebitForConsumption(150m, "DAILY-1", "over-consumption");

        Assert.Equal(-50m, account.Balance);
        Assert.Equal(-150m, transaction.Amount);
        Assert.Equal(-50m, transaction.BalanceAfter);
    }

    [Fact]
    public void Reconnect_WithPositiveBalance_Succeeds()
    {
        var account = PrepaidAccount.Open(Guid.NewGuid());
        account.Recharge(100m, "PAY-1");
        account.Disconnect();

        account.Reconnect();

        Assert.True(account.IsConnected);
    }

    [Fact]
    public void Reconnect_WithNonPositiveBalance_Throws()
    {
        var account = PrepaidAccount.Open(Guid.NewGuid());
        account.Disconnect();

        Assert.Throws<InvalidOperationException>(() => account.Reconnect());
    }

    [Fact]
    public void Disconnect_WhenAlreadyDisconnected_Throws()
    {
        var account = PrepaidAccount.Open(Guid.NewGuid());
        account.Disconnect();

        Assert.Throws<InvalidOperationException>(() => account.Disconnect());
    }

    [Fact]
    public void Adjust_BlankNote_Throws()
    {
        var account = PrepaidAccount.Open(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => account.Adjust(10m, "ADJ-1", " "));
    }
}
