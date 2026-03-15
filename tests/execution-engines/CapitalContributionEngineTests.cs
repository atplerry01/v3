namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Core.Capital;
using Whycespace.Contracts.Engines;

public sealed class CapitalContributionEngineTests
{
    private readonly CapitalContributionEngine _engine = new();

    // --- Helper factories ---

    private static Dictionary<string, object> ContributeInput(
        string? contributionId = null,
        string? poolId = null,
        string? investorIdentityId = null,
        string? commitmentId = null,
        decimal amount = 10_000m,
        string currency = "GBP",
        string paymentReference = "PAY-001",
        string? contributedBy = null)
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "ContributeCapital",
            ["contributionId"] = contributionId ?? Guid.NewGuid().ToString(),
            ["poolId"] = poolId ?? Guid.NewGuid().ToString(),
            ["investorIdentityId"] = investorIdentityId ?? Guid.NewGuid().ToString(),
            ["amount"] = amount,
            ["currency"] = currency,
            ["paymentReference"] = paymentReference
        };

        if (contributedBy is not null)
            data["contributedBy"] = contributedBy;
        if (commitmentId is not null)
            data["commitmentId"] = commitmentId;

        return data;
    }

    private static Dictionary<string, object> AdjustInput(
        string? contributionId = null,
        decimal adjustmentAmount = -500m,
        string reason = "Correction",
        string? adjustedBy = null)
    {
        return new Dictionary<string, object>
        {
            ["operation"] = "AdjustContribution",
            ["contributionId"] = contributionId ?? Guid.NewGuid().ToString(),
            ["adjustmentAmount"] = adjustmentAmount,
            ["reason"] = reason,
            ["adjustedBy"] = adjustedBy ?? Guid.NewGuid().ToString()
        };
    }

    private static Dictionary<string, object> ReverseInput(
        string? contributionId = null,
        string reason = "Fraudulent transfer",
        string? reversedBy = null)
    {
        return new Dictionary<string, object>
        {
            ["operation"] = "ReverseContribution",
            ["contributionId"] = contributionId ?? Guid.NewGuid().ToString(),
            ["reason"] = reason,
            ["reversedBy"] = reversedBy ?? Guid.NewGuid().ToString()
        };
    }

    private static EngineContext CreateContext(Dictionary<string, object> data, string step = "ContributeCapital")
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), step,
            "partition-1", data);
    }

    // --- RecordContribution ---

    [Fact]
    public async Task RecordContribution_ValidInput_Succeeds()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ContributeInput()));

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalContributed", result.Events[0].EventType);
        Assert.Equal("Recorded", result.Output["status"]);
    }

    [Fact]
    public async Task RecordContribution_EmitsEventWithCorrectPayload()
    {
        var poolId = Guid.NewGuid().ToString();
        var investorId = Guid.NewGuid().ToString();
        var result = await _engine.ExecuteAsync(CreateContext(
            ContributeInput(poolId: poolId, investorIdentityId: investorId, amount: 25_000m, currency: "USD")));

        Assert.True(result.Success);
        var evt = result.Events[0];
        Assert.Equal(poolId, evt.Payload["poolId"]);
        Assert.Equal(investorId, evt.Payload["investorIdentityId"]);
        Assert.Equal(25_000m, evt.Payload["amount"]);
        Assert.Equal("USD", evt.Payload["currency"]);
        Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task RecordContribution_EventTargetsPoolAggregate()
    {
        var poolId = Guid.NewGuid();
        var result = await _engine.ExecuteAsync(CreateContext(
            ContributeInput(poolId: poolId.ToString())));

        Assert.Equal(poolId, result.Events[0].AggregateId);
    }

    // --- ContributionWithCommitment ---

    [Fact]
    public async Task ContributionWithCommitment_IncludesCommitmentIdInEvent()
    {
        var commitmentId = Guid.NewGuid().ToString();
        var result = await _engine.ExecuteAsync(CreateContext(
            ContributeInput(commitmentId: commitmentId)));

        Assert.True(result.Success);
        Assert.Equal(commitmentId, result.Events[0].Payload["commitmentId"]);
    }

    [Fact]
    public async Task ContributionWithoutCommitment_OmitsCommitmentId()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ContributeInput()));

        Assert.True(result.Success);
        Assert.False(result.Events[0].Payload.ContainsKey("commitmentId"));
    }

    // --- AdjustContribution ---

    [Fact]
    public async Task AdjustContribution_ValidInput_Succeeds()
    {
        var result = await _engine.ExecuteAsync(CreateContext(AdjustInput(), "AdjustContribution"));

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalContributionAdjusted", result.Events[0].EventType);
        Assert.Equal("Adjusted", result.Output["status"]);
    }

    [Fact]
    public async Task AdjustContribution_EmitsCorrectPayload()
    {
        var contributionId = Guid.NewGuid().ToString();
        var result = await _engine.ExecuteAsync(CreateContext(
            AdjustInput(contributionId: contributionId, adjustmentAmount: -1000m, reason: "Overpayment")));

        var evt = result.Events[0];
        Assert.Equal(contributionId, evt.Payload["contributionId"]);
        Assert.Equal(-1000m, evt.Payload["adjustmentAmount"]);
        Assert.Equal("Overpayment", evt.Payload["reason"]);
        Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task AdjustContribution_ZeroAmount_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(
            AdjustInput(adjustmentAmount: 0m), "AdjustContribution"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AdjustContribution_MissingReason_Fails()
    {
        var data = AdjustInput();
        data.Remove("reason");
        var result = await _engine.ExecuteAsync(CreateContext(data, "AdjustContribution"));

        Assert.False(result.Success);
    }

    // --- ReverseContribution ---

    [Fact]
    public async Task ReverseContribution_ValidInput_Succeeds()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ReverseInput(), "ReverseContribution"));

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("CapitalContributionReversed", result.Events[0].EventType);
        Assert.Equal("Reversed", result.Output["status"]);
    }

    [Fact]
    public async Task ReverseContribution_EmitsCorrectPayload()
    {
        var contributionId = Guid.NewGuid().ToString();
        var result = await _engine.ExecuteAsync(CreateContext(
            ReverseInput(contributionId: contributionId, reason: "Compliance issue")));

        var evt = result.Events[0];
        Assert.Equal(contributionId, evt.Payload["contributionId"]);
        Assert.Equal("Compliance issue", evt.Payload["reason"]);
        Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task ReverseContribution_MissingReason_Fails()
    {
        var data = ReverseInput();
        data.Remove("reason");
        var result = await _engine.ExecuteAsync(CreateContext(data, "ReverseContribution"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReverseContribution_MissingReversedBy_Fails()
    {
        var data = ReverseInput();
        data.Remove("reversedBy");
        var result = await _engine.ExecuteAsync(CreateContext(data, "ReverseContribution"));

        Assert.False(result.Success);
    }

    // --- DuplicateContributionProtection (idempotent via deterministic IDs) ---

    [Fact]
    public async Task DuplicateContribution_SameContributionId_ProducesSameEventStructure()
    {
        var contributionId = Guid.NewGuid().ToString();
        var input = ContributeInput(contributionId: contributionId);
        var ctx = CreateContext(input);

        var result1 = await _engine.ExecuteAsync(ctx);
        var result2 = await _engine.ExecuteAsync(ctx);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
        Assert.Equal(result1.Events[0].Payload["contributionId"], result2.Events[0].Payload["contributionId"]);
    }

    // --- ConcurrentContributions ---

    [Fact]
    public async Task ConcurrentContributions_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _engine.ExecuteAsync(CreateContext(ContributeInput())))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        Assert.All(results, r => Assert.Single(r.Events));
    }

    // --- Validation ---

    [Fact]
    public async Task Validation_MissingContributionId_Fails()
    {
        var data = ContributeInput();
        data.Remove("contributionId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingPoolId_Fails()
    {
        var data = ContributeInput();
        data.Remove("poolId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingInvestorIdentityId_Fails()
    {
        var data = ContributeInput();
        data.Remove("investorIdentityId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingAmount_Fails()
    {
        var data = ContributeInput();
        data.Remove("amount");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Validation_InvalidAmount_Fails(decimal amount)
    {
        var result = await _engine.ExecuteAsync(CreateContext(ContributeInput(amount: amount)));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingCurrency_Fails()
    {
        var data = ContributeInput();
        data.Remove("currency");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_UnsupportedCurrency_Fails()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ContributeInput(currency: "BTC")));
        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("NGN")]
    public async Task AllSupportedCurrencies_Succeed(string currency)
    {
        var result = await _engine.ExecuteAsync(CreateContext(ContributeInput(currency: currency)));
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Validation_UnknownOperation_Fails()
    {
        var data = new Dictionary<string, object> { ["operation"] = "InvalidOp" };
        var result = await _engine.ExecuteAsync(CreateContext(data, "InvalidOp"));
        Assert.False(result.Success);
    }

    // --- Determinism ---

    [Fact]
    public async Task DeterministicExecution_SameInputSameStructure()
    {
        var context = CreateContext(ContributeInput());

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        for (int i = 0; i < result1.Events.Count; i++)
            Assert.Equal(result1.Events[i].EventType, result2.Events[i].EventType);
    }

    // --- All events target economic topic ---

    [Fact]
    public async Task AllOperations_EmitToEconomicTopic()
    {
        var contributeResult = await _engine.ExecuteAsync(CreateContext(ContributeInput()));
        var adjustResult = await _engine.ExecuteAsync(CreateContext(AdjustInput(), "AdjustContribution"));
        var reverseResult = await _engine.ExecuteAsync(CreateContext(ReverseInput(), "ReverseContribution"));

        foreach (var evt in contributeResult.Events)
            Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
        foreach (var evt in adjustResult.Events)
            Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
        foreach (var evt in reverseResult.Events)
            Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
    }
}
