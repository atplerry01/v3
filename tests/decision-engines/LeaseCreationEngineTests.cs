namespace Whycespace.DecisionEngines.Tests;

using Whycespace.Engines.T2E.Clusters.Property.Letting.Engines;
using Whycespace.Contracts.Engines;

public sealed class LeaseCreationEngineTests
{
    private readonly LeaseCreationEngine _engine = new();

    [Fact]
    public async Task ExecutesSuccessfully_WithValidInput()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateLease",
            "partition-1", new Dictionary<string, object>
            {
                ["tenantId"] = Guid.NewGuid().ToString(),
                ["propertyId"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("LeaseCreated", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("leaseId"));
    }

    [Fact]
    public async Task EmitsEvent_WithCorrectPayload()
    {
        var tenantId = Guid.NewGuid().ToString();
        var propertyId = Guid.NewGuid().ToString();

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateLease",
            "partition-1", new Dictionary<string, object>
            {
                ["tenantId"] = tenantId,
                ["propertyId"] = propertyId
            });

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("LeaseCreated", evt.EventType);
        Assert.Equal(tenantId, evt.Payload["tenantId"]);
        Assert.Equal(propertyId, evt.Payload["propertyId"]);
        Assert.Equal("whyce.property.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task Deterministic_SameStructureOnReplay()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateLease",
            "partition-1", new Dictionary<string, object>
            {
                ["tenantId"] = Guid.NewGuid().ToString(),
                ["propertyId"] = Guid.NewGuid().ToString()
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    [Fact]
    public async Task InvalidTenantId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateLease",
            "partition-1", new Dictionary<string, object>
            {
                ["tenantId"] = "not-a-guid",
                ["propertyId"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
