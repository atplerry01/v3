namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Economic.Capital.Engines;
using Whycespace.Contracts.Engines;

public sealed class CapitalAllocationEngineTests
{
    private readonly CapitalAllocationEngine _engine = new();

    private static Dictionary<string, object> ValidAllocateInput(
        string? allocationId = null,
        string? reservationId = null,
        string? poolId = null,
        string? targetType = null,
        string? targetId = null,
        decimal amount = 50000m,
        string currency = "GBP",
        string? allocatedBy = null)
    {
        return new Dictionary<string, object>
        {
            ["action"] = "allocate",
            ["allocationId"] = allocationId ?? Guid.NewGuid().ToString(),
            ["reservationId"] = reservationId ?? Guid.NewGuid().ToString(),
            ["poolId"] = poolId ?? Guid.NewGuid().ToString(),
            ["targetType"] = targetType ?? "SPV",
            ["targetId"] = targetId ?? Guid.NewGuid().ToString(),
            ["amount"] = amount,
            ["currency"] = currency,
            ["allocatedBy"] = allocatedBy ?? Guid.NewGuid().ToString()
        };
    }

    private static Dictionary<string, object> ValidCancelInput(
        string? allocationId = null,
        string? reason = null,
        string? cancelledBy = null)
    {
        return new Dictionary<string, object>
        {
            ["action"] = "cancel",
            ["allocationId"] = allocationId ?? Guid.NewGuid().ToString(),
            ["reason"] = reason ?? "Budget reallocation",
            ["cancelledBy"] = cancelledBy ?? Guid.NewGuid().ToString()
        };
    }

    private static Dictionary<string, object> ValidReassignInput(
        string? allocationId = null,
        string? newTargetType = null,
        string? newTargetId = null,
        string? reassignedBy = null)
    {
        return new Dictionary<string, object>
        {
            ["action"] = "reassign",
            ["allocationId"] = allocationId ?? Guid.NewGuid().ToString(),
            ["newTargetType"] = newTargetType ?? "Asset",
            ["newTargetId"] = newTargetId ?? Guid.NewGuid().ToString(),
            ["reassignedBy"] = reassignedBy ?? Guid.NewGuid().ToString()
        };
    }

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AllocateCapital",
            "partition-1", data);
    }

    // --- AllocateCapital ---

    [Fact]
    public async Task AllocateCapital_ValidInput_Succeeds()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidAllocateInput()));

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.True(result.Output.ContainsKey("allocationId"));
        Assert.Equal("Allocated", result.Output["status"]);
    }

    [Fact]
    public async Task AllocateCapital_EmitsCorrectEventSequence()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidAllocateInput()));

        Assert.Equal("CapitalAllocated", result.Events[0].EventType);
        Assert.Equal("ReservationConsumed", result.Events[1].EventType);
    }

    [Fact]
    public async Task AllocateCapital_AllEventsTargetCapitalTopic()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidAllocateInput()));

        foreach (var evt in result.Events)
        {
            Assert.Equal("whyce.capital.events", evt.Payload["topic"]);
        }
    }

    [Fact]
    public async Task AllocateCapital_EventContainsAllRequiredFields()
    {
        var reservationId = Guid.NewGuid().ToString();
        var poolId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();

        var result = await _engine.ExecuteAsync(CreateContext(ValidAllocateInput(
            reservationId: reservationId, poolId: poolId, targetId: targetId,
            targetType: "Project", amount: 75000m, currency: "USD")));

        var allocated = result.Events[0];
        Assert.Equal(reservationId, allocated.Payload["reservationId"]);
        Assert.Equal(poolId, allocated.Payload["poolId"]);
        Assert.Equal("Project", allocated.Payload["targetType"]);
        Assert.Equal(targetId, allocated.Payload["targetId"]);
        Assert.Equal(75000m, allocated.Payload["amount"]);
        Assert.Equal("USD", allocated.Payload["currency"]);
    }

    [Fact]
    public async Task AllocateCapital_OutputContainsAllocatedStatus()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidAllocateInput()));

        Assert.Equal("Allocated", result.Output["status"]);
        Assert.True(result.Output.ContainsKey("allocatedAt"));
    }

    [Fact]
    public async Task AllocateCapital_GeneratesAllocationId_WhenNotProvided()
    {
        var data = ValidAllocateInput();
        data.Remove("allocationId");

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.True(Guid.TryParse(result.Output["allocationId"] as string, out _));
    }

    // --- AllocateCapital Validation ---

    [Fact]
    public async Task Validation_MissingReservationId_Fails()
    {
        var data = ValidAllocateInput();
        data.Remove("reservationId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingPoolId_Fails()
    {
        var data = ValidAllocateInput();
        data.Remove("poolId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingTargetType_Fails()
    {
        var data = ValidAllocateInput();
        data.Remove("targetType");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_InvalidTargetType_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidAllocateInput(targetType: "Unknown")));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingTargetId_Fails()
    {
        var data = ValidAllocateInput();
        data.Remove("targetId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingAmount_Fails()
    {
        var data = ValidAllocateInput();
        data.Remove("amount");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_ZeroAmount_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidAllocateInput(amount: 0m)));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_NegativeAmount_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidAllocateInput(amount: -500m)));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingCurrency_Fails()
    {
        var data = ValidAllocateInput();
        data.Remove("currency");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_UnsupportedCurrency_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidAllocateInput(currency: "BTC")));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingAllocatedBy_Fails()
    {
        var data = ValidAllocateInput();
        data.Remove("allocatedBy");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    // --- All valid target types ---

    [Theory]
    [InlineData("SPV")]
    [InlineData("Asset")]
    [InlineData("Project")]
    [InlineData("OperationalProgram")]
    public async Task AllValidTargetTypes_Succeed(string targetType)
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidAllocateInput(targetType: targetType)));
        Assert.True(result.Success);
    }

    // --- CancelAllocation ---

    [Fact]
    public async Task CancelAllocation_ValidInput_Succeeds()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidCancelInput()));

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalAllocationCancelled", result.Events[0].EventType);
        Assert.Equal("Cancelled", result.Output["status"]);
    }

    [Fact]
    public async Task CancelAllocation_EventContainsReason()
    {
        var allocationId = Guid.NewGuid().ToString();
        var result = await _engine.ExecuteAsync(CreateContext(
            ValidCancelInput(allocationId: allocationId, reason: "Project cancelled")));

        var evt = result.Events[0];
        Assert.Equal(allocationId, evt.Payload["allocationId"]);
        Assert.Equal("Project cancelled", evt.Payload["reason"]);
    }

    [Fact]
    public async Task CancelAllocation_MissingReason_Fails()
    {
        var data = ValidCancelInput();
        data.Remove("reason");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CancelAllocation_MissingCancelledBy_Fails()
    {
        var data = ValidCancelInput();
        data.Remove("cancelledBy");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    // --- ReassignAllocation ---

    [Fact]
    public async Task ReassignAllocation_ValidInput_Succeeds()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidReassignInput()));

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalAllocationReassigned", result.Events[0].EventType);
        Assert.Equal("Reassigned", result.Output["status"]);
    }

    [Fact]
    public async Task ReassignAllocation_EventContainsNewTarget()
    {
        var newTargetId = Guid.NewGuid().ToString();
        var result = await _engine.ExecuteAsync(CreateContext(
            ValidReassignInput(newTargetType: "Project", newTargetId: newTargetId)));

        var evt = result.Events[0];
        Assert.Equal("Project", evt.Payload["newTargetType"]);
        Assert.Equal(newTargetId, evt.Payload["newTargetId"]);
    }

    [Fact]
    public async Task ReassignAllocation_InvalidNewTargetType_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(
            ValidReassignInput(newTargetType: "Invalid")));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReassignAllocation_MissingNewTargetId_Fails()
    {
        var data = ValidReassignInput();
        data.Remove("newTargetId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReassignAllocation_MissingReassignedBy_Fails()
    {
        var data = ValidReassignInput();
        data.Remove("reassignedBy");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    // --- DuplicateAllocationProtection ---

    [Fact]
    public async Task DuplicateAllocation_SameIdProducesSameStructure()
    {
        var allocationId = Guid.NewGuid().ToString();
        var input = ValidAllocateInput(allocationId: allocationId);

        var result1 = await _engine.ExecuteAsync(CreateContext(new Dictionary<string, object>(input)));
        var result2 = await _engine.ExecuteAsync(CreateContext(new Dictionary<string, object>(input)));

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Output["allocationId"], result2.Output["allocationId"]);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
    }

    // --- ConcurrentAllocations ---

    [Fact]
    public async Task ConcurrentAllocations_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _engine.ExecuteAsync(CreateContext(ValidAllocateInput())))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        var allocationIds = results.Select(r => r.Output["allocationId"] as string).ToArray();
        Assert.Equal(allocationIds.Distinct().Count(), allocationIds.Length);
    }

    // --- Determinism ---

    [Fact]
    public async Task DeterministicExecution_SameEventTypes()
    {
        var context = CreateContext(ValidAllocateInput());

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        for (int i = 0; i < result1.Events.Count; i++)
            Assert.Equal(result1.Events[i].EventType, result2.Events[i].EventType);
    }

    // --- Unknown action ---

    [Fact]
    public async Task UnknownAction_Fails()
    {
        var data = new Dictionary<string, object> { ["action"] = "invalid" };
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }
}
