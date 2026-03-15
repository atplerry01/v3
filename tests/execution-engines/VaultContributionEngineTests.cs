namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Core.Vault;
using Whycespace.Contracts.Engines;

public sealed class VaultContributionEngineTests
{
    private readonly VaultContributionEngine _engine = new();

    private static Dictionary<string, object> ValidInput(
        string? contributionId = null,
        string? vaultId = null,
        string? vaultAccountId = null,
        string? contributorIdentityId = null,
        decimal amount = 5000m,
        string currency = "GBP",
        string contributionSource = "Investor",
        string description = "Initial investment")
    {
        return new Dictionary<string, object>
        {
            ["contributionId"] = contributionId ?? Guid.NewGuid().ToString(),
            ["vaultId"] = vaultId ?? Guid.NewGuid().ToString(),
            ["vaultAccountId"] = vaultAccountId ?? Guid.NewGuid().ToString(),
            ["contributorIdentityId"] = contributorIdentityId ?? Guid.NewGuid().ToString(),
            ["amount"] = amount,
            ["currency"] = currency,
            ["contributionSource"] = contributionSource,
            ["description"] = description
        };
    }

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteContribution",
            "partition-1", data);
    }

    // --- ExecuteContributionSuccessTest ---

    [Fact]
    public async Task ExecuteContribution_ValidInput_Succeeds()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput()));

        Assert.True(result.Success);
        Assert.Equal(3, result.Events.Count);
        Assert.True(result.Output.ContainsKey("contributionId"));
        Assert.True(result.Output.ContainsKey("transactionId"));
        Assert.Equal("Completed", result.Output["transactionStatus"]);
    }

    [Fact]
    public async Task ExecuteContribution_EmitsCorrectEventSequence()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput()));

        Assert.Equal("VaultContributionReceived", result.Events[0].EventType);
        Assert.Equal("VaultContributionProcessed", result.Events[1].EventType);
        Assert.Equal("VaultContributionCompleted", result.Events[2].EventType);
    }

    [Fact]
    public async Task ExecuteContribution_AllEventsTargetEconomicTopic()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput()));

        foreach (var evt in result.Events)
        {
            Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
        }
    }

    // --- ContributionValidationTest ---

    [Fact]
    public async Task Validation_MissingContributionId_Fails()
    {
        var data = ValidInput();
        data.Remove("contributionId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_InvalidAmount_Zero_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput(amount: 0m)));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_InvalidAmount_Negative_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput(amount: -100m)));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingVaultId_Fails()
    {
        var data = ValidInput();
        data.Remove("vaultId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingCurrency_Fails()
    {
        var data = ValidInput();
        data.Remove("currency");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_UnsupportedCurrency_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput(currency: "BTC")));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingContributorIdentityId_Fails()
    {
        var data = ValidInput();
        data.Remove("contributorIdentityId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_InvalidContributionSource_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput(contributionSource: "Unknown")));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingVaultAccountId_Fails()
    {
        var data = ValidInput();
        data.Remove("vaultAccountId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    // --- LedgerIntegrationTest ---

    [Fact]
    public async Task LedgerEvent_ContainsCreditDirection()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput()));

        var processedEvent = result.Events[1];
        Assert.Equal("VaultContributionProcessed", processedEvent.EventType);
        Assert.Equal("Credit", processedEvent.Payload["ledgerDirection"]);
        Assert.Equal("Contribution", processedEvent.Payload["ledgerTransactionType"]);
    }

    [Fact]
    public async Task LedgerEvent_ContainsCorrectAmountAndCurrency()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput(amount: 7500m, currency: "USD")));

        var processedEvent = result.Events[1];
        Assert.Equal(7500m, processedEvent.Payload["amount"]);
        Assert.Equal("USD", processedEvent.Payload["currency"]);
    }

    // --- RegistryIntegrationTest ---

    [Fact]
    public async Task CompletedEvent_ContainsTransactionRegistryData()
    {
        var contributionId = Guid.NewGuid().ToString();
        var vaultId = Guid.NewGuid().ToString();

        var result = await _engine.ExecuteAsync(CreateContext(
            ValidInput(contributionId: contributionId, vaultId: vaultId)));

        var completedEvent = result.Events[2];
        Assert.Equal("VaultContributionCompleted", completedEvent.EventType);
        Assert.Equal(contributionId, completedEvent.Payload["contributionId"]);
        Assert.Equal(vaultId, completedEvent.Payload["vaultId"]);
        Assert.Equal("Completed", completedEvent.Payload["transactionStatus"]);
        Assert.True(completedEvent.Payload.ContainsKey("completedAt"));
    }

    [Fact]
    public async Task Output_ContainsTransactionId()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput()));

        Assert.True(result.Output.ContainsKey("transactionId"));
        Assert.True(Guid.TryParse(result.Output["transactionId"] as string, out _));
    }

    // --- ParticipantValidationTest ---

    [Fact]
    public async Task ContributorIdentity_IncludedInReceivedEvent()
    {
        var contributorId = Guid.NewGuid().ToString();
        var result = await _engine.ExecuteAsync(CreateContext(
            ValidInput(contributorIdentityId: contributorId)));

        var receivedEvent = result.Events[0];
        Assert.Equal(contributorId, receivedEvent.Payload["contributorIdentityId"]);
    }

    [Fact]
    public async Task ContributorIdentity_IncludedInCompletedEvent()
    {
        var contributorId = Guid.NewGuid().ToString();
        var result = await _engine.ExecuteAsync(CreateContext(
            ValidInput(contributorIdentityId: contributorId)));

        var completedEvent = result.Events[2];
        Assert.Equal(contributorId, completedEvent.Payload["contributorIdentityId"]);
    }

    // --- Determinism ---

    [Fact]
    public async Task DeterministicExecution_SameStructure()
    {
        var context = CreateContext(ValidInput());

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        for (int i = 0; i < result1.Events.Count; i++)
            Assert.Equal(result1.Events[i].EventType, result2.Events[i].EventType);
    }

    // --- Optional reference fields ---

    [Fact]
    public async Task OptionalReferenceFields_IncludedInCompletedEvent()
    {
        var data = ValidInput();
        data["referenceId"] = "REF-001";
        data["referenceType"] = "ExternalTransfer";

        var result = await _engine.ExecuteAsync(CreateContext(data));

        var completedEvent = result.Events[2];
        Assert.Equal("REF-001", completedEvent.Payload["referenceId"]);
        Assert.Equal("ExternalTransfer", completedEvent.Payload["referenceType"]);
    }

    // --- All contribution sources ---

    [Theory]
    [InlineData("Investor")]
    [InlineData("Treasury")]
    [InlineData("Revenue")]
    [InlineData("Grant")]
    public async Task AllValidContributionSources_Succeed(string source)
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidInput(contributionSource: source)));
        Assert.True(result.Success);
    }
}
