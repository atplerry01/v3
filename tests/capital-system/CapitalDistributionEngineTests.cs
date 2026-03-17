namespace Whycespace.CapitalSystem.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Engines.T2E.Economic.Capital.Engines;

public sealed class CapitalDistributionEngineTests
{
    private readonly CapitalDistributionEngine _engine = new();

    private static EngineContext CreateDistributeContext(
        string? distributionId = null,
        string? poolId = null,
        string? targetType = null,
        string? targetId = null,
        object? totalAmount = null,
        string? currency = null,
        string? distributionType = null,
        string? distributedBy = null)
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "DistributeCapital",
            ["distributionId"] = distributionId ?? Guid.NewGuid().ToString(),
            ["poolId"] = poolId ?? Guid.NewGuid().ToString(),
            ["targetType"] = targetType ?? "SPV",
            ["targetId"] = targetId ?? Guid.NewGuid().ToString(),
            ["totalAmount"] = totalAmount ?? 100000m,
            ["currency"] = currency ?? "GBP",
            ["distributionType"] = distributionType ?? "ProfitDistribution",
            ["distributedBy"] = distributedBy ?? Guid.NewGuid().ToString()
        };

        return new EngineContext(
            Guid.NewGuid(), "capital-distribution", "DistributeCapital",
            new PartitionKey("whyce.capital"),
            data);
    }

    private static string GetError(EngineResult result) => result.Output["error"] as string ?? "";

    // --- DistributeCapital Tests ---

    [Fact]
    public async Task DistributeCapital_ValidCommand_ProducesEvent()
    {
        var context = CreateDistributeContext();

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalDistributed", result.Events[0].EventType);
    }

    [Fact]
    public async Task DistributeCapital_ValidCommand_ReturnsDistributedStatus()
    {
        var context = CreateDistributeContext();

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Distributed", result.Output["status"]);
        Assert.Contains("Capital distributed", result.Output["message"] as string);
    }

    [Fact]
    public async Task DistributeCapital_EventContainsAllRequiredFields()
    {
        var distributionId = Guid.NewGuid().ToString();
        var poolId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();
        var context = CreateDistributeContext(
            distributionId: distributionId,
            poolId: poolId,
            targetType: "Investor",
            targetId: targetId,
            totalAmount: 250000m,
            currency: "USD",
            distributionType: "DividendDistribution");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var evt = result.Events[0];
        Assert.Equal(distributionId, evt.Payload["distributionId"]);
        Assert.Equal(poolId, evt.Payload["poolId"]);
        Assert.Equal("Investor", evt.Payload["targetType"]);
        Assert.Equal(targetId, evt.Payload["targetId"]);
        Assert.Equal(250000m, evt.Payload["totalAmount"]);
        Assert.Equal("USD", evt.Payload["currency"]);
        Assert.Equal("DividendDistribution", evt.Payload["distributionType"]);
    }

    [Fact]
    public async Task DistributeCapital_MissingDistributionId_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "DistributeCapital",
            ["poolId"] = Guid.NewGuid().ToString(),
            ["targetType"] = "SPV",
            ["targetId"] = Guid.NewGuid().ToString(),
            ["totalAmount"] = 100000m,
            ["currency"] = "GBP",
            ["distributionType"] = "ProfitDistribution",
            ["distributedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-distribution", "DistributeCapital",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("distributionId", GetError(result));
    }

    [Fact]
    public async Task DistributeCapital_MissingPoolId_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "DistributeCapital",
            ["distributionId"] = Guid.NewGuid().ToString(),
            ["targetType"] = "SPV",
            ["targetId"] = Guid.NewGuid().ToString(),
            ["totalAmount"] = 100000m,
            ["currency"] = "GBP",
            ["distributionType"] = "ProfitDistribution",
            ["distributedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-distribution", "DistributeCapital",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("poolId", GetError(result));
    }

    [Fact]
    public async Task DistributeCapital_InvalidTargetType_Fails()
    {
        var context = CreateDistributeContext(targetType: "InvalidType");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Invalid targetType", GetError(result));
    }

    [Fact]
    public async Task DistributeCapital_ZeroAmount_Fails()
    {
        var context = CreateDistributeContext(totalAmount: 0m);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("positive", GetError(result));
    }

    [Fact]
    public async Task DistributeCapital_NegativeAmount_Fails()
    {
        var context = CreateDistributeContext(totalAmount: -50000m);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("positive", GetError(result));
    }

    [Fact]
    public async Task DistributeCapital_UnsupportedCurrency_Fails()
    {
        var context = CreateDistributeContext(currency: "JPY");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Unsupported currency", GetError(result));
    }

    [Fact]
    public async Task DistributeCapital_InvalidDistributionType_Fails()
    {
        var context = CreateDistributeContext(distributionType: "InvalidType");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Invalid distributionType", GetError(result));
    }

    [Fact]
    public async Task DistributeCapital_AllValidDistributionTypes_Succeed()
    {
        foreach (var distType in new[] { "ProfitDistribution", "ReturnOfCapital", "DividendDistribution", "LiquidationDistribution" })
        {
            var context = CreateDistributeContext(distributionType: distType);
            var result = await _engine.ExecuteAsync(context);
            Assert.True(result.Success, $"Expected success for distributionType: {distType}");
        }
    }

    [Fact]
    public async Task DistributeCapital_AllValidTargetTypes_Succeed()
    {
        foreach (var targetType in new[] { "Pool", "SPV", "CWG", "Investor" })
        {
            var context = CreateDistributeContext(targetType: targetType);
            var result = await _engine.ExecuteAsync(context);
            Assert.True(result.Success, $"Expected success for targetType: {targetType}");
        }
    }

    // --- AdjustDistribution Tests ---

    [Fact]
    public async Task AdjustDistribution_ValidCommand_ProducesEvent()
    {
        var distributionId = Guid.NewGuid().ToString();
        var data = new Dictionary<string, object>
        {
            ["operation"] = "AdjustDistribution",
            ["distributionId"] = distributionId,
            ["adjustmentAmount"] = -5000m,
            ["reason"] = "Calculation correction",
            ["adjustedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-distribution", "AdjustDistribution",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalDistributionAdjusted", result.Events[0].EventType);
        Assert.Equal("Adjusted", result.Output["status"]);
    }

    [Fact]
    public async Task AdjustDistribution_ZeroAdjustment_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "AdjustDistribution",
            ["distributionId"] = Guid.NewGuid().ToString(),
            ["adjustmentAmount"] = 0m,
            ["reason"] = "No change",
            ["adjustedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-distribution", "AdjustDistribution",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("zero", GetError(result));
    }

    [Fact]
    public async Task AdjustDistribution_MissingReason_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "AdjustDistribution",
            ["distributionId"] = Guid.NewGuid().ToString(),
            ["adjustmentAmount"] = 1000m,
            ["adjustedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-distribution", "AdjustDistribution",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("reason", GetError(result));
    }

    // --- ReverseDistribution Tests ---

    [Fact]
    public async Task ReverseDistribution_ValidCommand_ProducesEvent()
    {
        var distributionId = Guid.NewGuid().ToString();
        var data = new Dictionary<string, object>
        {
            ["operation"] = "ReverseDistribution",
            ["distributionId"] = distributionId,
            ["reason"] = "Distribution error",
            ["reversedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-distribution", "ReverseDistribution",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalDistributionReversed", result.Events[0].EventType);
        Assert.Equal("Reversed", result.Output["status"]);
    }

    [Fact]
    public async Task ReverseDistribution_MissingReason_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "ReverseDistribution",
            ["distributionId"] = Guid.NewGuid().ToString(),
            ["reversedBy"] = Guid.NewGuid().ToString()
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-distribution", "ReverseDistribution",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("reason", GetError(result));
    }

    [Fact]
    public async Task ReverseDistribution_MissingReversedBy_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "ReverseDistribution",
            ["distributionId"] = Guid.NewGuid().ToString(),
            ["reason"] = "Test reversal"
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-distribution", "ReverseDistribution",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("reversedBy", GetError(result));
    }

    // --- General Tests ---

    [Fact]
    public async Task UnknownOperation_Fails()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "InvalidOperation"
        };
        var context = new EngineContext(
            Guid.NewGuid(), "capital-distribution", "InvalidOperation",
            new PartitionKey("whyce.capital"), data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Unknown operation", GetError(result));
    }

    [Fact]
    public async Task DistributeCapital_DuplicateIdempotent_ProducesSameEventType()
    {
        var distributionId = Guid.NewGuid().ToString();
        var context1 = CreateDistributeContext(distributionId: distributionId);
        var context2 = CreateDistributeContext(distributionId: distributionId);

        var result1 = await _engine.ExecuteAsync(context1);
        var result2 = await _engine.ExecuteAsync(context2);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    [Fact]
    public async Task ConcurrentDistributions_AreThreadSafe()
    {
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var context = CreateDistributeContext();
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
        var engine = new CapitalDistributionEngine();
        Assert.Equal("CapitalDistribution", engine.Name);
    }
}
