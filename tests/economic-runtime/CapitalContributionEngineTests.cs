namespace Whycespace.EconomicRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Engines.T2E.Economic.Capital.Engines;

public sealed class CapitalContributionEngineTests
{
    [Fact]
    public async Task RecordContribution_ProducesEvent()
    {
        var engine = new CapitalContributionEngine();
        var spvId = Guid.NewGuid();
        var vaultId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), spvId.ToString(), "RecordContribution",
            new PartitionKey("whyce.economic"),
            new Dictionary<string, object>
            {
                ["spvId"] = spvId.ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["amount"] = 50000m
            });

        var result = await engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("CapitalContributed", result.Events[0].EventType);
    }
}
