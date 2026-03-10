namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E_Execution;
using Whycespace.Shared.Contracts;
using Xunit;

public sealed class VaultCreationEngineTests
{
    private readonly VaultCreationEngine _engine = new();

    [Fact]
    public async Task ValidOwner_CreatesVault()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "GBP",
                ["initialBalance"] = 10000m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultCreated");
        Assert.True(result.Output.ContainsKey("vaultId"));
    }

    [Fact]
    public async Task MissingOwnerId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object> { ["currency"] = "GBP" });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UnsupportedCurrency_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "BTC"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task NegativeBalance_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["initialBalance"] = -100m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }
}
