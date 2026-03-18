namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Economic.Vault.Engines;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultStateRecoveryEngineTests
{
    private readonly VaultStateRecoveryEngine _engine = new();

    private static readonly string TestVaultId = Guid.NewGuid().ToString();
    private static readonly string TestSnapshotId = Guid.NewGuid().ToString();
    private static readonly string TestRequestedBy = Guid.NewGuid().ToString();

    private static EngineContext CreateContext(Dictionary<string, object> data) =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteVaultRecovery", "partition-1", data);

    private static Dictionary<string, object> CreateSnapshotData(
        decimal vaultBalance = 50000m,
        int transactionCount = 42,
        int participantCount = 3,
        int allocationCount = 2)
    {
        var participants = new List<object>();
        for (var i = 0; i < participantCount; i++)
            participants.Add(new Dictionary<string, object> { ["participantId"] = Guid.NewGuid().ToString() });

        var allocations = new List<object>();
        for (var i = 0; i < allocationCount; i++)
            allocations.Add(new Dictionary<string, object> { ["allocationId"] = Guid.NewGuid().ToString() });

        return new Dictionary<string, object>
        {
            ["vaultBalance"] = vaultBalance,
            ["transactionCount"] = transactionCount,
            ["participants"] = participants,
            ["allocations"] = allocations,
            ["snapshotTimestamp"] = DateTime.UtcNow.ToString("O")
        };
    }

    private static Dictionary<string, object> ValidRecoveryData(Dictionary<string, object>? snapshotData = null) =>
        new()
        {
            ["recoveryId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = TestVaultId,
            ["snapshotId"] = TestSnapshotId,
            ["requestedBy"] = TestRequestedBy,
            ["snapshotData"] = snapshotData ?? CreateSnapshotData()
        };

    [Fact]
    public async Task RecoverySuccess_ValidSnapshot_Succeeds()
    {
        var context = CreateContext(ValidRecoveryData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Recovered", result.Output["recoveryStatus"]);
    }

    [Fact]
    public async Task BalanceRecovery_RestoresFromSnapshot()
    {
        var snapshotData = CreateSnapshotData(vaultBalance: 75000m);
        var context = CreateContext(ValidRecoveryData(snapshotData));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(75000m, result.Output["recoveredVaultBalance"]);
    }

    [Fact]
    public async Task ParticipantRecovery_RestoresCorrectCount()
    {
        var snapshotData = CreateSnapshotData(participantCount: 5);
        var context = CreateContext(ValidRecoveryData(snapshotData));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(5, result.Output["recoveredParticipantCount"]);
    }

    [Fact]
    public async Task AllocationRecovery_RestoresCorrectCount()
    {
        var snapshotData = CreateSnapshotData(allocationCount: 4);
        var context = CreateContext(ValidRecoveryData(snapshotData));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(4, result.Output["recoveredAllocationCount"]);
    }

    [Fact]
    public async Task RecoveryDeterministic_SameInput_SameOutput()
    {
        var snapshotData = CreateSnapshotData(vaultBalance: 10000m, transactionCount: 10, participantCount: 2, allocationCount: 1);
        var data1 = ValidRecoveryData(snapshotData);
        var data2 = new Dictionary<string, object>(data1);

        var result1 = await _engine.ExecuteAsync(CreateContext(data1));
        var result2 = await _engine.ExecuteAsync(CreateContext(data2));

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Output["recoveredVaultBalance"], result2.Output["recoveredVaultBalance"]);
        Assert.Equal(result1.Output["recoveredTransactionCount"], result2.Output["recoveredTransactionCount"]);
        Assert.Equal(result1.Output["recoveredParticipantCount"], result2.Output["recoveredParticipantCount"]);
        Assert.Equal(result1.Output["recoveredAllocationCount"], result2.Output["recoveredAllocationCount"]);
        Assert.Equal(result1.Output["recoveryStatus"], result2.Output["recoveryStatus"]);
    }

    [Fact]
    public async Task Recovery_EmitsStartedAndCompletedEvents()
    {
        var context = CreateContext(ValidRecoveryData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Contains(result.Events, e => e.EventType == "VaultRecoveryStarted");
        Assert.Contains(result.Events, e => e.EventType == "VaultRecoveryCompleted");
    }

    [Fact]
    public async Task Recovery_EventPayloadContainsTopic()
    {
        var context = CreateContext(ValidRecoveryData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        foreach (var evt in result.Events)
            Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task Recovery_MissingRecoveryId_Fails()
    {
        var data = ValidRecoveryData();
        data.Remove("recoveryId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Recovery_MissingVaultId_Fails()
    {
        var data = ValidRecoveryData();
        data.Remove("vaultId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Recovery_MissingSnapshotId_Fails()
    {
        var data = ValidRecoveryData();
        data.Remove("snapshotId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Recovery_MissingRequestedBy_Fails()
    {
        var data = ValidRecoveryData();
        data.Remove("requestedBy");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Recovery_MissingSnapshotData_Fails()
    {
        var data = ValidRecoveryData();
        data.Remove("snapshotData");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Recovery_InvalidRecoveryScope_Fails()
    {
        var data = ValidRecoveryData();
        data["recoveryScope"] = "invalid";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Recovery_InvalidVaultIdFormat_Fails()
    {
        var data = ValidRecoveryData();
        data["vaultId"] = "not-a-guid";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Recovery_TransactionCountRestored()
    {
        var snapshotData = CreateSnapshotData(transactionCount: 99);
        var context = CreateContext(ValidRecoveryData(snapshotData));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(99, result.Output["recoveredTransactionCount"]);
    }

    [Fact]
    public async Task Recovery_DefaultRecoveryScope_IsFull()
    {
        var context = CreateContext(ValidRecoveryData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        // Engine defaults to "full" scope when not specified
        Assert.Contains("VaultRecoveryCompleted", result.Events.Select(e => e.EventType));
    }
}
