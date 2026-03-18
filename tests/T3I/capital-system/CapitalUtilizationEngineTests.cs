namespace Whycespace.CapitalSystem.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Engines.T2E.Economic.Capital.Engines;

public sealed class CapitalUtilizationEngineTests
{
    private readonly CapitalUtilizationEngine _engine = new();

    private static EngineContext CreateUtilizeContext(
        string? utilizationId = null,
        string? allocationId = null,
        string? poolId = null,
        string? targetType = null,
        string? targetId = null,
        object? amount = null,
        string? currency = null,
        string? referenceId = null,
        string? utilizedBy = null)
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "UtilizeCapital",
            ["utilizationId"] = utilizationId ?? Guid.NewGuid().ToString(),
            ["allocationId"] = allocationId ?? Guid.NewGuid().ToString(),
            ["poolId"] = poolId ?? Guid.NewGuid().ToString(),
            ["targetType"] = targetType ?? "SPV",
            ["targetId"] = targetId ?? Guid.NewGuid().ToString(),
            ["amount"] = amount ?? 50000m,
            ["currency"] = currency ?? "GBP",
            ["referenceId"] = referenceId ?? Guid.NewGuid().ToString(),
            ["utilizedBy"] = utilizedBy ?? Guid.NewGuid().ToString()
        };

        return new EngineContext(
            Guid.NewGuid(), "capital-utilization", "UtilizeCapital",
            new PartitionKey("whyce.capital"),
            data);
    }

    private static string GetError(EngineResult result) => result.Output["error"] as string ?? "";

    [Fact]
    public async Task UtilizeCapital_ValidCommand_ProducesEvents()
    {
        var context = CreateUtilizeContext();

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("CapitalUtilized", result.Events[0].EventType);
        Assert.Equal("AllocationConsumed", result.Events[1].EventType);
    }

    [Fact]
    public async Task UtilizeCapital_ValidCommand_ReturnsUtilizedStatus()
    {
        var context = CreateUtilizeContext();

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Utilized", result.Output["status"]);
        Assert.Equal("Capital utilization recorded", result.Output["message"]);
    }

    [Fact]
    public async Task UtilizeCapital_MissingUtilizationId_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "UtilizeCapital",
            ["allocationId"] = Guid.NewGuid().ToString(),
            ["poolId"] = Guid.NewGuid().ToString(),
            ["targetType"] = "SPV",
            ["targetId"] = Guid.NewGuid().ToString(),
            ["amount"] = 50000m,
            ["currency"] = "GBP",
            ["utilizedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-utilization", "UtilizeCapital",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("utilizationId", GetError(result));
    }

    [Fact]
    public async Task UtilizeCapital_InvalidTargetType_Fails()
    {
        var context = CreateUtilizeContext(targetType: "InvalidType");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Invalid targetType", GetError(result));
    }

    [Fact]
    public async Task UtilizeCapital_ZeroAmount_Fails()
    {
        var context = CreateUtilizeContext(amount: 0m);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("positive", GetError(result));
    }

    [Fact]
    public async Task UtilizeCapital_NegativeAmount_Fails()
    {
        var context = CreateUtilizeContext(amount: -100m);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("positive", GetError(result));
    }

    [Fact]
    public async Task UtilizeCapital_UnsupportedCurrency_Fails()
    {
        var context = CreateUtilizeContext(currency: "JPY");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Unsupported currency", GetError(result));
    }

    [Fact]
    public async Task UtilizeCapital_AllValidTargetTypes_Succeed()
    {
        foreach (var targetType in new[] { "SPV", "Asset", "Project", "OperationalProgram" })
        {
            var context = CreateUtilizeContext(targetType: targetType);
            var result = await _engine.ExecuteAsync(context);
            Assert.True(result.Success, $"Expected success for targetType: {targetType}");
        }
    }

    [Fact]
    public async Task AdjustUtilization_ValidCommand_ProducesEvent()
    {
        var utilizationId = Guid.NewGuid().ToString();
        var data = new Dictionary<string, object>
        {
            ["operation"] = "AdjustUtilization",
            ["utilizationId"] = utilizationId,
            ["adjustmentAmount"] = -5000m,
            ["reason"] = "Overspend correction",
            ["adjustedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-utilization", "AdjustUtilization",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalUtilizationAdjusted", result.Events[0].EventType);
        Assert.Equal("Adjusted", result.Output["status"]);
    }

    [Fact]
    public async Task AdjustUtilization_ZeroAdjustment_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "AdjustUtilization",
            ["utilizationId"] = Guid.NewGuid().ToString(),
            ["adjustmentAmount"] = 0m,
            ["reason"] = "No change",
            ["adjustedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-utilization", "AdjustUtilization",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("zero", GetError(result));
    }

    [Fact]
    public async Task AdjustUtilization_MissingReason_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "AdjustUtilization",
            ["utilizationId"] = Guid.NewGuid().ToString(),
            ["adjustmentAmount"] = 1000m,
            ["adjustedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-utilization", "AdjustUtilization",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("reason", GetError(result));
    }

    [Fact]
    public async Task ReverseUtilization_ValidCommand_ProducesEvent()
    {
        var utilizationId = Guid.NewGuid().ToString();
        var data = new Dictionary<string, object>
        {
            ["operation"] = "ReverseUtilization",
            ["utilizationId"] = utilizationId,
            ["reason"] = "Asset acquisition cancelled",
            ["reversedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-utilization", "ReverseUtilization",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalUtilizationReversed", result.Events[0].EventType);
        Assert.Equal("Reversed", result.Output["status"]);
    }

    [Fact]
    public async Task ReverseUtilization_MissingReason_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "ReverseUtilization",
            ["utilizationId"] = Guid.NewGuid().ToString(),
            ["reversedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-utilization", "ReverseUtilization",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("reason", GetError(result));
    }

    [Fact]
    public async Task ReverseUtilization_MissingReversedBy_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "ReverseUtilization",
            ["utilizationId"] = Guid.NewGuid().ToString(),
            ["reason"] = "Test reversal"
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-utilization", "ReverseUtilization",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("reversedBy", GetError(result));
    }

    [Fact]
    public async Task UnknownOperation_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "InvalidOperation"
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-utilization", "InvalidOperation",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Unknown operation", GetError(result));
    }

    [Fact]
    public async Task UtilizeCapital_DuplicateIdempotent_ProducesSameEventType()
    {
        var utilizationId = Guid.NewGuid().ToString();
        var context1 = CreateUtilizeContext(utilizationId: utilizationId);
        var context2 = CreateUtilizeContext(utilizationId: utilizationId);

        var result1 = await _engine.ExecuteAsync(context1);
        var result2 = await _engine.ExecuteAsync(context2);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    [Fact]
    public async Task ConcurrentUtilizations_AreThreadSafe()
    {
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var context = CreateUtilizeContext();
                    var result = await _engine.ExecuteAsync(context);
                    Assert.True(result.Success);
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
    }

    [Fact]
    public void Engine_IsStateless()
    {
        var engine = new CapitalUtilizationEngine();
        Assert.Equal("CapitalUtilization", engine.Name);
    }

    [Fact]
    public async Task UtilizeCapital_EventContainsAllRequiredFields()
    {
        var utilizationId = Guid.NewGuid().ToString();
        var allocationId = Guid.NewGuid().ToString();
        var poolId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();
        var context = CreateUtilizeContext(
            utilizationId: utilizationId,
            allocationId: allocationId,
            poolId: poolId,
            targetId: targetId,
            amount: 75000m,
            currency: "USD");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var evt = result.Events[0];
        Assert.Equal(utilizationId, evt.Payload["utilizationId"]);
        Assert.Equal(allocationId, evt.Payload["allocationId"]);
        Assert.Equal(poolId, evt.Payload["poolId"]);
        Assert.Equal("SPV", evt.Payload["targetType"]);
        Assert.Equal(targetId, evt.Payload["targetId"]);
        Assert.Equal(75000m, evt.Payload["amount"]);
        Assert.Equal("USD", evt.Payload["currency"]);
    }
}
