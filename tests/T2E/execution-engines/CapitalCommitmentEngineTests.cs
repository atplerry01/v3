namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Economic.Capital.Commitment.Engines;
using Whycespace.Contracts.Engines;

public sealed class CapitalCommitmentEngineTests
{
    private readonly CapitalCommitmentEngine _engine = new();

    // --- CommitCapital ---

    [Fact]
    public async Task CommitCapital_ValidInput_ReturnsSuccess()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CommitCapital",
            "partition-1", new Dictionary<string, object>
            {
                ["poolId"] = Guid.NewGuid().ToString(),
                ["investorIdentityId"] = Guid.NewGuid().ToString(),
                ["amount"] = 100000m,
                ["currency"] = "GBP",
                ["committedBy"] = "system-admin"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalCommitted", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("commitmentId"));
        Assert.Equal("Pending", result.Output["status"]);
    }

    [Fact]
    public async Task CommitCapital_EmitsEventWithCorrectPayload()
    {
        var poolId = Guid.NewGuid().ToString();
        var investorId = Guid.NewGuid().ToString();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CommitCapital",
            "partition-1", new Dictionary<string, object>
            {
                ["poolId"] = poolId,
                ["investorIdentityId"] = investorId,
                ["amount"] = 50000m,
                ["currency"] = "USD",
                ["committedBy"] = "admin"
            });

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("CapitalCommitted", evt.EventType);
        Assert.Equal(poolId, evt.Payload["poolId"]);
        Assert.Equal(investorId, evt.Payload["investorIdentityId"]);
        Assert.Equal(50000m, evt.Payload["amount"]);
        Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task CommitCapital_MissingPoolId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CommitCapital",
            "partition-1", new Dictionary<string, object>
            {
                ["investorIdentityId"] = Guid.NewGuid().ToString(),
                ["amount"] = 100000m,
                ["committedBy"] = "admin"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CommitCapital_NegativeAmount_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CommitCapital",
            "partition-1", new Dictionary<string, object>
            {
                ["poolId"] = Guid.NewGuid().ToString(),
                ["investorIdentityId"] = Guid.NewGuid().ToString(),
                ["amount"] = -500m,
                ["currency"] = "GBP",
                ["committedBy"] = "admin"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CommitCapital_UnsupportedCurrency_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CommitCapital",
            "partition-1", new Dictionary<string, object>
            {
                ["poolId"] = Guid.NewGuid().ToString(),
                ["investorIdentityId"] = Guid.NewGuid().ToString(),
                ["amount"] = 100000m,
                ["currency"] = "XYZ",
                ["committedBy"] = "admin"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- UpdateCommitment ---

    [Fact]
    public async Task UpdateCommitment_ValidInput_ReturnsSuccess()
    {
        var commitmentId = Guid.NewGuid().ToString();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "UpdateCapitalCommitment",
            "partition-1", new Dictionary<string, object>
            {
                ["commitmentId"] = commitmentId,
                ["newAmount"] = 150000m,
                ["updatedBy"] = "admin"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalCommitmentUpdated", result.Events[0].EventType);
        Assert.Equal("Updated", result.Output["status"]);
    }

    [Fact]
    public async Task UpdateCommitment_MissingNewAmount_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "UpdateCapitalCommitment",
            "partition-1", new Dictionary<string, object>
            {
                ["commitmentId"] = Guid.NewGuid().ToString(),
                ["updatedBy"] = "admin"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- CancelCommitment ---

    [Fact]
    public async Task CancelCommitment_ValidInput_ReturnsSuccess()
    {
        var commitmentId = Guid.NewGuid().ToString();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CancelCapitalCommitment",
            "partition-1", new Dictionary<string, object>
            {
                ["commitmentId"] = commitmentId,
                ["reason"] = "Investor withdrew",
                ["cancelledBy"] = "admin"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalCommitmentCancelled", result.Events[0].EventType);
        Assert.Equal("Cancelled", result.Output["status"]);
        Assert.Equal("Investor withdrew", result.Output["reason"]);
    }

    [Fact]
    public async Task CancelCommitment_MissingReason_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CancelCapitalCommitment",
            "partition-1", new Dictionary<string, object>
            {
                ["commitmentId"] = Guid.NewGuid().ToString(),
                ["cancelledBy"] = "admin"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- FulfillCommitment ---

    [Fact]
    public async Task FulfillCommitment_ValidInput_ReturnsSuccess()
    {
        var commitmentId = Guid.NewGuid().ToString();
        var contributionId = Guid.NewGuid().ToString();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "FulfillCapitalCommitment",
            "partition-1", new Dictionary<string, object>
            {
                ["commitmentId"] = commitmentId,
                ["contributionId"] = contributionId,
                ["fulfilledBy"] = "system"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalCommitmentFulfilled", result.Events[0].EventType);
        Assert.Equal("Fulfilled", result.Output["status"]);
        Assert.Equal(contributionId, result.Output["contributionId"]);
    }

    [Fact]
    public async Task FulfillCommitment_MissingContributionId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "FulfillCapitalCommitment",
            "partition-1", new Dictionary<string, object>
            {
                ["commitmentId"] = Guid.NewGuid().ToString(),
                ["fulfilledBy"] = "system"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- DuplicateCommitmentProtection ---

    [Fact]
    public async Task CommitCapital_WithExplicitCommitmentId_UsesProvidedId()
    {
        var commitmentId = Guid.NewGuid().ToString();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CommitCapital",
            "partition-1", new Dictionary<string, object>
            {
                ["commitmentId"] = commitmentId,
                ["poolId"] = Guid.NewGuid().ToString(),
                ["investorIdentityId"] = Guid.NewGuid().ToString(),
                ["amount"] = 100000m,
                ["currency"] = "GBP",
                ["committedBy"] = "admin"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(commitmentId, result.Output["commitmentId"]);
    }

    // --- ConcurrentCommitments ---

    [Fact]
    public async Task ConcurrentCommitments_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 10).Select(_ =>
        {
            var context = new EngineContext(
                Guid.NewGuid(), Guid.NewGuid().ToString(), "CommitCapital",
                "partition-1", new Dictionary<string, object>
                {
                    ["poolId"] = Guid.NewGuid().ToString(),
                    ["investorIdentityId"] = Guid.NewGuid().ToString(),
                    ["amount"] = 50000m,
                    ["currency"] = "GBP",
                    ["committedBy"] = "admin"
                });
            return _engine.ExecuteAsync(context);
        });

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        Assert.All(results, r => Assert.Single(r.Events));

        var commitmentIds = results.Select(r => r.Output["commitmentId"]).ToList();
        Assert.Equal(commitmentIds.Distinct().Count(), commitmentIds.Count);
    }

    // --- Deterministic Execution ---

    [Fact]
    public async Task DeterministicExecution_SameStructure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CommitCapital",
            "partition-1", new Dictionary<string, object>
            {
                ["poolId"] = Guid.NewGuid().ToString(),
                ["investorIdentityId"] = Guid.NewGuid().ToString(),
                ["amount"] = 100000m,
                ["currency"] = "GBP",
                ["committedBy"] = "admin"
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    // --- Unknown Step ---

    [Fact]
    public async Task UnknownStep_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "UnknownStep",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Idempotent Failure ---

    [Fact]
    public async Task Idempotent_MissingCommittedBy_AlwaysFails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CommitCapital",
            "partition-1", new Dictionary<string, object>
            {
                ["poolId"] = Guid.NewGuid().ToString(),
                ["investorIdentityId"] = Guid.NewGuid().ToString(),
                ["amount"] = 100000m,
                ["currency"] = "GBP"
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.False(result1.Success);
        Assert.False(result2.Success);
    }
}
