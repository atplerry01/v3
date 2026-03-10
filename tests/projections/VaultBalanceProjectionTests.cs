namespace Whycespace.Tests.Projections;

using Whycespace.Runtime.Projections;
using Whycespace.Shared.Events;
using Xunit;

public sealed class VaultBalanceProjectionTests
{
    [Fact]
    public async Task HandleAsync_CapitalAllocated_DeductsBalance()
    {
        var projection = new VaultBalanceProjection();
        var vaultId = Guid.NewGuid();
        var @event = new SystemEvent(
            Guid.NewGuid(), "CapitalAllocated", vaultId,
            DateTimeOffset.UtcNow, new Dictionary<string, object>
            {
                ["amount"] = 5000m
            });

        await projection.HandleAsync(@event);
        var balances = projection.GetBalances();

        Assert.Equal(-5000m, balances[vaultId.ToString()]);
    }

    [Fact]
    public async Task HandleAsync_ProfitDistributed_AddsBalance()
    {
        var projection = new VaultBalanceProjection();
        var vaultId = Guid.NewGuid();
        var @event = new SystemEvent(
            Guid.NewGuid(), "ProfitDistributed", vaultId,
            DateTimeOffset.UtcNow, new Dictionary<string, object>
            {
                ["amount"] = 3000m
            });

        await projection.HandleAsync(@event);
        var balances = projection.GetBalances();

        Assert.Equal(3000m, balances[vaultId.ToString()]);
    }
}
