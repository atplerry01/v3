namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E_Execution;
using Whycespace.Shared.Contracts;
using Xunit;

public sealed class CapitalContributionEngineTests
{
    private readonly CapitalContributionEngine _engine = new();

    [Fact]
    public async Task ValidContribution_Succeeds()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Contribute",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["amount"] = 5000m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Contains(result.Events, e => e.EventType == "CapitalContributed");
        Assert.Contains(result.Events, e => e.EventType == "VaultDebited");
    }

    [Fact]
    public async Task MissingSpvId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Contribute",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["amount"] = 5000m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ZeroAmount_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Contribute",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["amount"] = 0m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }
}
