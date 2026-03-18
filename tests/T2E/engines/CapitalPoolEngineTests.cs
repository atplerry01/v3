namespace Whycespace.Tests.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Engines.T2E.Economic.Capital.Allocation.Engines;
using Xunit;

public sealed class CapitalPoolEngineTests
{
    private readonly CapitalPoolEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
        => new(Guid.NewGuid(), Guid.NewGuid().ToString(), "CapitalPool",
            new PartitionKey("whyce.economic"), data);

    // --- CreateCapitalPool ---

    [Fact]
    public async Task CreateCapitalPool_WithValidInput_Succeeds()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Create",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["poolName"] = "Strategic Investment Pool Alpha",
            ["clusterId"] = Guid.NewGuid().ToString(),
            ["subClusterId"] = Guid.NewGuid().ToString(),
            ["spvId"] = Guid.NewGuid().ToString(),
            ["currency"] = "GBP",
            ["createdBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalPoolCreated", result.Events[0].EventType);
        Assert.Equal("Created", result.Output["poolStatus"]);
    }

    [Fact]
    public async Task CreateCapitalPool_MissingPoolName_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Create",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["clusterId"] = Guid.NewGuid().ToString(),
            ["subClusterId"] = Guid.NewGuid().ToString(),
            ["spvId"] = Guid.NewGuid().ToString(),
            ["currency"] = "GBP",
            ["createdBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateCapitalPool_InvalidCurrency_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Create",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["poolName"] = "Test Pool",
            ["clusterId"] = Guid.NewGuid().ToString(),
            ["subClusterId"] = Guid.NewGuid().ToString(),
            ["spvId"] = Guid.NewGuid().ToString(),
            ["currency"] = "INVALID",
            ["createdBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("INVALID", result.Output["error"] as string);
    }

    // --- ActivatePool ---

    [Fact]
    public async Task ActivatePool_FromCreatedStatus_Succeeds()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Activate",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["currentPoolStatus"] = "Created",
            ["activatedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalPoolActivated", result.Events[0].EventType);
        Assert.Equal("Active", result.Output["poolStatus"]);
    }

    [Fact]
    public async Task ActivatePool_FromActiveStatus_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Activate",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["currentPoolStatus"] = "Active",
            ["activatedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- SuspendPool ---

    [Fact]
    public async Task SuspendPool_WithValidInput_Succeeds()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Suspend",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["currentPoolStatus"] = "Active",
            ["reason"] = "Regulatory review pending",
            ["suspendedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalPoolSuspended", result.Events[0].EventType);
        Assert.Equal("Suspended", result.Output["poolStatus"]);
    }

    [Fact]
    public async Task SuspendPool_NotActive_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Suspend",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["currentPoolStatus"] = "Created",
            ["reason"] = "Some reason",
            ["suspendedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SuspendPool_MissingReason_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Suspend",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["currentPoolStatus"] = "Active",
            ["suspendedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- ClosePool ---

    [Fact]
    public async Task ClosePool_WithValidInput_Succeeds()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Close",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["currentPoolStatus"] = "Active",
            ["closedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalPoolClosed", result.Events[0].EventType);
        Assert.Equal("Closed", result.Output["poolStatus"]);
    }

    [Fact]
    public async Task ClosePool_AlreadyClosed_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Close",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["currentPoolStatus"] = "Closed",
            ["closedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- DuplicatePoolCreation (idempotent — same poolId yields deterministic event) ---

    [Fact]
    public async Task DuplicatePoolCreation_SamePoolId_ProducesDeterministicEvents()
    {
        var poolId = Guid.NewGuid().ToString();
        var data = new Dictionary<string, object>
        {
            ["action"] = "Create",
            ["poolId"] = poolId,
            ["poolName"] = "Determinism Test Pool",
            ["clusterId"] = Guid.NewGuid().ToString(),
            ["subClusterId"] = Guid.NewGuid().ToString(),
            ["spvId"] = Guid.NewGuid().ToString(),
            ["currency"] = "USD",
            ["createdBy"] = Guid.NewGuid().ToString()
        };

        var result1 = await _engine.ExecuteAsync(CreateContext(data));
        var result2 = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
        Assert.Equal(result1.Events[0].AggregateId, result2.Events[0].AggregateId);
    }

    // --- ConcurrentCommandExecution ---

    [Fact]
    public async Task ConcurrentCommandExecution_ThreadSafe()
    {
        var tasks = Enumerable.Range(0, 50).Select(_ =>
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                ["action"] = "Create",
                ["poolId"] = Guid.NewGuid().ToString(),
                ["poolName"] = $"Concurrent Pool {Guid.NewGuid()}",
                ["clusterId"] = Guid.NewGuid().ToString(),
                ["subClusterId"] = Guid.NewGuid().ToString(),
                ["spvId"] = Guid.NewGuid().ToString(),
                ["currency"] = "EUR",
                ["createdBy"] = Guid.NewGuid().ToString()
            });
            return _engine.ExecuteAsync(context);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        Assert.Equal(50, results.Length);
    }

    // --- Unknown action ---

    [Fact]
    public async Task UnknownAction_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Delete",
            ["poolId"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Event payload contains topic ---

    [Fact]
    public async Task CreateCapitalPool_EventContainsTopic()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "Create",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["poolName"] = "Topic Test Pool",
            ["clusterId"] = Guid.NewGuid().ToString(),
            ["subClusterId"] = Guid.NewGuid().ToString(),
            ["spvId"] = Guid.NewGuid().ToString(),
            ["currency"] = "GBP",
            ["createdBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("whyce.economic.events", result.Events[0].Payload["topic"]);
    }
}
