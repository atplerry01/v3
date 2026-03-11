namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Core.Vault;
using Whycespace.Contracts.Engines;

public sealed class VaultCreationEngineTests
{
    private readonly VaultCreationEngine _engine = new();

    [Fact]
    public async Task ExecutesSuccessfully_WithValidInput()
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
        Assert.Single(result.Events);
        Assert.Equal("VaultCreated", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("vaultId"));
    }

    [Fact]
    public async Task ProducesDomainEvent_WithCorrectPayload()
    {
        var ownerId = Guid.NewGuid().ToString();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["ownerId"] = ownerId,
                ["currency"] = "USD",
                ["initialBalance"] = 5000m
            });

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("VaultCreated", evt.EventType);
        Assert.Equal(ownerId, evt.Payload["ownerId"]);
        Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task DeterministicExecution_SameStructure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "GBP",
                ["initialBalance"] = 100m
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    [Fact]
    public async Task Idempotent_MissingOwnerId_AlwaysFails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object> { ["currency"] = "GBP" });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.False(result1.Success);
        Assert.False(result2.Success);
    }
}
