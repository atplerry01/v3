namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Core.Vault;
using Whycespace.Contracts.Engines;

public sealed class VaultGovernanceRuleEngineTests
{
    private readonly VaultGovernanceRuleEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ValidateGovernance",
            "partition-1", data);
    }

    private static Dictionary<string, object> ValidData(decimal amount = 1000m) => new()
    {
        ["vaultId"] = Guid.NewGuid().ToString(),
        ["vaultAccountId"] = Guid.NewGuid().ToString(),
        ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
        ["operationType"] = "Withdrawal",
        ["amount"] = amount,
        ["governanceScope"] = "GeneralPurpose",
        ["requestedAt"] = DateTime.UtcNow.ToString("O")
    };

    // --- Governance Approval Tests ---

    [Fact]
    public async Task ApproveOperation_WhenGovernanceRulesSatisfied()
    {
        var data = ValidData(5000m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isApproved"]);
        Assert.Equal("Approved", result.Output["governanceDecision"]);
    }

    [Fact]
    public async Task EmitsRequestedAndApprovedEvents_WhenApproved()
    {
        var data = ValidData(500m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.Equal(2, result.Events.Count);
        Assert.Equal("VaultGovernanceValidationRequested", result.Events[0].EventType);
        Assert.Equal("VaultGovernanceValidationApproved", result.Events[1].EventType);
        Assert.Equal("whyce.economic.events", result.Events[1].Payload["topic"]);
    }

    // --- Governance Threshold Tests ---

    [Fact]
    public async Task RequireGovernanceApproval_WhenAboveStandardThreshold()
    {
        var data = ValidData(150_000m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isApproved"]);
        Assert.Equal("GovernanceApprovalRequired", result.Output["governanceDecision"]);
    }

    [Fact]
    public async Task ApproveAboveThreshold_WhenGovernanceApprovalGranted()
    {
        var data = ValidData(150_000m);
        data["governanceApproval"] = "true";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isApproved"]);
        Assert.Equal("Approved", result.Output["governanceDecision"]);
    }

    // --- Multi-Party Approval Tests ---

    [Fact]
    public async Task RequireMultiPartyApproval_WhenAboveMultiPartyThreshold()
    {
        var data = ValidData(300_000m);
        data["governanceApproval"] = "true";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isApproved"]);
        Assert.Equal("MultiPartyApprovalRequired", result.Output["governanceDecision"]);
    }

    [Fact]
    public async Task ApproveMultiParty_WhenQuorumSatisfied()
    {
        var data = ValidData(300_000m);
        data["governanceApproval"] = "true";
        data["approvalCount"] = 2;
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isApproved"]);
    }

    [Fact]
    public async Task RejectMultiParty_WhenInsufficientApprovals()
    {
        var data = ValidData(300_000m);
        data["governanceApproval"] = "true";
        data["approvalCount"] = 1;
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isApproved"]);
        Assert.Equal("MultiPartyApprovalRequired", result.Output["governanceDecision"]);
    }

    // --- Guardian Oversight Tests ---

    [Fact]
    public async Task RequireGuardianOversight_WhenAboveGuardianThreshold()
    {
        var data = ValidData(2_000_000m);
        data["governanceApproval"] = "true";
        data["approvalCount"] = 3;
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isApproved"]);
        Assert.Equal("GuardianOversightRequired", result.Output["governanceDecision"]);
    }

    [Fact]
    public async Task ApproveGuardian_WhenQuorumSatisfied()
    {
        var data = ValidData(2_000_000m);
        data["governanceApproval"] = "true";
        data["approvalCount"] = 3;
        data["guardianQuorumSatisfied"] = "true";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isApproved"]);
        Assert.Equal("Approved", result.Output["governanceDecision"]);
    }

    // --- Governance Rejection Tests ---

    [Fact]
    public async Task RejectOperation_WhenScopeRestrictionViolated()
    {
        var data = ValidData(500m);
        data["governanceScope"] = "InfrastructureFunding";
        data["operationType"] = "ProfitDistribution";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isApproved"]);
        Assert.Equal("Rejected", result.Output["governanceDecision"]);
    }

    [Fact]
    public async Task RejectEscrowProfitDistribution()
    {
        var data = ValidData(500m);
        data["governanceScope"] = "Escrow";
        data["operationType"] = "ProfitDistribution";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isApproved"]);
        Assert.Equal("Rejected", result.Output["governanceDecision"]);
    }

    [Fact]
    public async Task EmitsRejectedEvent_WhenGovernanceFails()
    {
        var data = ValidData(150_000m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.Equal(2, result.Events.Count);
        Assert.Equal("VaultGovernanceValidationRequested", result.Events[0].EventType);
        Assert.Equal("VaultGovernanceValidationRejected", result.Events[1].EventType);
    }

    // --- Input Validation Tests ---

    [Fact]
    public async Task FailOnMissingVaultId()
    {
        var data = ValidData();
        data.Remove("vaultId");
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task FailOnInvalidOperationType()
    {
        var data = ValidData();
        data["operationType"] = "InvalidOp";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task FailOnNegativeAmount()
    {
        var data = ValidData();
        data["amount"] = -100m;
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task FailOnInvalidGovernanceScope()
    {
        var data = ValidData();
        data["governanceScope"] = "InvalidScope";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    // --- Deterministic Execution ---

    [Fact]
    public async Task DeterministicExecution_SameStructure()
    {
        var context = CreateContext(ValidData(500m));

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        Assert.Equal(result1.Output["isApproved"], result2.Output["isApproved"]);
        Assert.Equal(result1.Output["governanceDecision"], result2.Output["governanceDecision"]);
    }
}
