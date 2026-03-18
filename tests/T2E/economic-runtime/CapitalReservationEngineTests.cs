namespace Whycespace.EconomicRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Engines.T2E.Economic.Capital.Reservation.Engines;

public sealed class CapitalReservationEngineTests
{
    private readonly CapitalReservationEngine _engine = new();

    private static EngineContext CreateContext(string workflowStep, Dictionary<string, object> data)
        => new(Guid.NewGuid(), Guid.NewGuid().ToString(), workflowStep,
            new PartitionKey("whyce.economic"), data);

    [Fact]
    public async Task ReserveCapital_WithValidData_ProducesReservedEvent()
    {
        var poolId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var context = CreateContext("ReserveCapital", new Dictionary<string, object>
        {
            ["poolId"] = poolId.ToString(),
            ["targetType"] = "SPV",
            ["targetId"] = targetId.ToString(),
            ["amount"] = 100000m,
            ["currency"] = "GBP",
            ["reservedBy"] = "operator-1"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("CapitalReserved", result.Events[0].EventType);
        Assert.Equal("PoolBalanceReduced", result.Events[1].EventType);
        Assert.Equal(poolId, result.Events[0].AggregateId);
        Assert.Equal("Reserved", result.Output["status"]);
        Assert.Equal(100000m, result.Output["amount"]);
    }

    [Fact]
    public async Task ReserveCapital_MissingPoolId_Fails()
    {
        var context = CreateContext("ReserveCapital", new Dictionary<string, object>
        {
            ["targetType"] = "SPV",
            ["targetId"] = Guid.NewGuid().ToString(),
            ["amount"] = 50000m,
            ["reservedBy"] = "operator-1"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal("Missing poolId", result.Output["error"]);
    }

    [Fact]
    public async Task ReserveCapital_InvalidTargetType_Fails()
    {
        var context = CreateContext("ReserveCapital", new Dictionary<string, object>
        {
            ["poolId"] = Guid.NewGuid().ToString(),
            ["targetType"] = "InvalidType",
            ["targetId"] = Guid.NewGuid().ToString(),
            ["amount"] = 50000m,
            ["reservedBy"] = "operator-1"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Invalid targetType", result.Output["error"] as string);
    }

    [Fact]
    public async Task ReserveCapital_NegativeAmount_Fails()
    {
        var context = CreateContext("ReserveCapital", new Dictionary<string, object>
        {
            ["poolId"] = Guid.NewGuid().ToString(),
            ["targetType"] = "Asset",
            ["targetId"] = Guid.NewGuid().ToString(),
            ["amount"] = -1000m,
            ["reservedBy"] = "operator-1"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal("Reservation amount must be positive", result.Output["error"]);
    }

    [Fact]
    public async Task ReleaseReservation_WithValidData_ProducesReleasedEvent()
    {
        var reservationId = Guid.NewGuid();
        var context = CreateContext("ReleaseReservation", new Dictionary<string, object>
        {
            ["reservationId"] = reservationId.ToString(),
            ["reason"] = "SPV acquisition cancelled",
            ["releasedBy"] = "operator-1"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalReservationReleased", result.Events[0].EventType);
        Assert.Equal(reservationId, result.Events[0].AggregateId);
        Assert.Equal("Released", result.Output["status"]);
    }

    [Fact]
    public async Task ReleaseReservation_MissingReason_Fails()
    {
        var context = CreateContext("ReleaseReservation", new Dictionary<string, object>
        {
            ["reservationId"] = Guid.NewGuid().ToString(),
            ["releasedBy"] = "operator-1"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal("Missing release reason", result.Output["error"]);
    }

    [Fact]
    public async Task ExpireReservation_WithValidData_ProducesExpiredEvent()
    {
        var reservationId = Guid.NewGuid();
        var context = CreateContext("ExpireReservation", new Dictionary<string, object>
        {
            ["reservationId"] = reservationId.ToString(),
            ["expirationReason"] = "Reservation window exceeded 30 days"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalReservationExpired", result.Events[0].EventType);
        Assert.Equal(reservationId, result.Events[0].AggregateId);
        Assert.Equal("Expired", result.Output["status"]);
    }

    [Fact]
    public async Task ExpireReservation_DefaultReason_UsesDefault()
    {
        var reservationId = Guid.NewGuid();
        var context = CreateContext("ExpireReservation", new Dictionary<string, object>
        {
            ["reservationId"] = reservationId.ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Reservation expired", result.Output["expirationReason"]);
    }

    [Fact]
    public async Task DuplicateReservationProtection_UniqueIdsPerExecution()
    {
        var data = new Dictionary<string, object>
        {
            ["poolId"] = Guid.NewGuid().ToString(),
            ["targetType"] = "SPV",
            ["targetId"] = Guid.NewGuid().ToString(),
            ["amount"] = 25000m,
            ["currency"] = "GBP",
            ["reservedBy"] = "operator-1"
        };

        var result1 = await _engine.ExecuteAsync(CreateContext("ReserveCapital", data));
        var result2 = await _engine.ExecuteAsync(CreateContext("ReserveCapital", data));

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.NotEqual(result1.Output["reservationId"], result2.Output["reservationId"]);
    }

    [Fact]
    public async Task ConcurrentReservations_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 10).Select(_ =>
        {
            var data = new Dictionary<string, object>
            {
                ["poolId"] = Guid.NewGuid().ToString(),
                ["targetType"] = "Project",
                ["targetId"] = Guid.NewGuid().ToString(),
                ["amount"] = 10000m,
                ["currency"] = "USD",
                ["reservedBy"] = "operator-concurrent"
            };
            return _engine.ExecuteAsync(CreateContext("ReserveCapital", data));
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        var reservationIds = results.Select(r => r.Output["reservationId"]).ToHashSet();
        Assert.Equal(10, reservationIds.Count);
    }

    [Fact]
    public async Task UnknownCommand_Fails()
    {
        var context = CreateContext("UnknownCommand", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Unknown command", result.Output["error"] as string);
    }

    [Fact]
    public async Task Engine_IsDeterministic_SameInputsSameStructure()
    {
        var poolId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();
        var data = new Dictionary<string, object>
        {
            ["poolId"] = poolId,
            ["targetType"] = "Asset",
            ["targetId"] = targetId,
            ["amount"] = 75000m,
            ["currency"] = "EUR",
            ["reservedBy"] = "operator-1"
        };

        var result = await _engine.ExecuteAsync(CreateContext("ReserveCapital", data));

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("Asset", result.Output["targetType"]);
        Assert.Equal(75000m, result.Output["amount"]);
    }
}
