namespace Whycespace.DecisionEngines.Tests;

using Whycespace.Engines.T2E.Clusters.Property.Letting.Engines;
using Whycespace.Contracts.Engines;

public sealed class TenantMatchingEngineTests
{
    private readonly TenantMatchingEngine _engine = new();

    [Fact]
    public async Task ExecutesSuccessfully_WithValidListing()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "MatchTenant",
            "partition-1", new Dictionary<string, object>
            {
                ["listingId"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("TenantMatched", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("tenantId"));
    }

    [Fact]
    public async Task EmitsEvent_WithListingId()
    {
        var listingId = Guid.NewGuid().ToString();

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "MatchTenant",
            "partition-1", new Dictionary<string, object>
            {
                ["listingId"] = listingId
            });

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("TenantMatched", evt.EventType);
        Assert.Equal(listingId, evt.Payload["listingId"]);
    }

    [Fact]
    public async Task Deterministic_SameStructureOnReplay()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "MatchTenant",
            "partition-1", new Dictionary<string, object>
            {
                ["listingId"] = Guid.NewGuid().ToString()
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    [Fact]
    public async Task MissingListingId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "MatchTenant",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
