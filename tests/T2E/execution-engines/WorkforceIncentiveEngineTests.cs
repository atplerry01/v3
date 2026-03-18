namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Workforce.Engines;
using Whycespace.Engines.T2E.Workforce.Models;
using Whycespace.Domain.Clusters.Operations.Shared;
using Whycespace.Contracts.Engines;

public sealed class WorkforceIncentiveEngineTests
{
    private readonly WorkforceIncentiveEngine _engine = new();

    private static readonly DateTimeOffset PeriodStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 3, 31, 23, 59, 59, TimeSpan.Zero);

    [Fact]
    public async Task EligibleWorker_StandardTier_ReturnsIncentive()
    {
        var context = CreateContext(
            performanceScore: 65m,
            performanceTier: "Standard",
            baseIncentiveAmount: 1000m);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkforceIncentiveApproved", result.Events[0].EventType);
        Assert.Equal(1000m, result.Output["incentiveAmount"]);
    }

    [Fact]
    public async Task HighTier_AppliesMultiplier()
    {
        var context = CreateContext(
            performanceScore: 80m,
            performanceTier: "High",
            baseIncentiveAmount: 1000m);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(1250m, result.Output["incentiveAmount"]);
    }

    [Fact]
    public async Task ExceptionalTier_AppliesMultiplier()
    {
        var context = CreateContext(
            performanceScore: 95m,
            performanceTier: "Exceptional",
            baseIncentiveAmount: 1000m);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(1500m, result.Output["incentiveAmount"]);
    }

    [Fact]
    public async Task LowTier_ZeroIncentive()
    {
        var context = CreateContext(
            performanceScore: 30m,
            performanceTier: "Low",
            baseIncentiveAmount: 1000m);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(0m, result.Output["incentiveAmount"]);
    }

    [Fact]
    public async Task SuspendedWorker_ReturnsRejection()
    {
        var context = CreateContext(
            performanceScore: 80m,
            performanceTier: "High",
            baseIncentiveAmount: 1000m,
            workerStatus: "Suspended");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DisabledWorker_ReturnsRejection()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "Worker", new[] { "TaxiRide" });

        var command = new WorkforceIncentiveCommand(
            workforce.WorkerId, 80m, "High",
            PeriodStart, PeriodEnd, 1000m, "USD", "PerformanceBonus");

        // Suspend then check - disabled workers not eligible
        workforce.Suspend();
        var decision = WorkforceIncentiveEngine.EvaluateIncentive(workforce, command);

        Assert.False(decision.Eligible);
        Assert.Contains("Suspended", decision.Reason);
    }

    [Fact]
    public async Task InvalidPerformanceTier_ReturnsRejection()
    {
        var context = CreateContext(
            performanceScore: 80m,
            performanceTier: "Unknown",
            baseIncentiveAmount: 1000m);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ZeroBaseIncentive_ReturnsZeroAmount()
    {
        var context = CreateContext(
            performanceScore: 95m,
            performanceTier: "Exceptional",
            baseIncentiveAmount: 0m);

        var result = await _engine.ExecuteAsync(context);

        // 0 * 1.5 = 0, which results in EngineResult.Fail since amount is 0
        // Actually the engine returns Fail when !decision.Eligible
        // Let's check: 0 * 1.5 = 0, eligible = true, amount = 0
        // The engine succeeds with 0 amount
        Assert.True(result.Success);
        Assert.Equal(0m, result.Output["incentiveAmount"]);
    }

    [Fact]
    public async Task InvalidEvaluationPeriod_ReturnsRejection()
    {
        var context = CreateContext(
            performanceScore: 80m,
            performanceTier: "High",
            baseIncentiveAmount: 1000m,
            periodStart: PeriodEnd,
            periodEnd: PeriodStart);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task NegativeBaseAmount_ReturnsRejection()
    {
        var context = CreateContext(
            performanceScore: 80m,
            performanceTier: "High",
            baseIncentiveAmount: -500m);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingRequiredFields_ReturnsFailure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Incentive",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void EvaluateIncentive_Directly_Success()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "TopWorker", new[] { "TaxiRide" });

        var command = new WorkforceIncentiveCommand(
            workforce.WorkerId, 92m, "Exceptional",
            PeriodStart, PeriodEnd, 2000m, "GBP", "PerformanceBonus");

        var decision = WorkforceIncentiveEngine.EvaluateIncentive(workforce, command);

        Assert.True(decision.Eligible);
        Assert.Equal(3000m, decision.IncentiveAmount);
        Assert.Equal("GBP", decision.Currency);
        Assert.Equal("PerformanceBonus", decision.IncentiveType);
        Assert.StartsWith("PAYOUT-", decision.PayoutReference);
    }

    [Fact]
    public void EvaluateIncentive_DeterministicPayoutReference()
    {
        var workerId = WorkerId.New();
        var workforce = WorkforceAggregate.Register(workerId, "Worker", new[] { "TaxiRide" });

        var command = new WorkforceIncentiveCommand(
            workforce.WorkerId, 80m, "High",
            PeriodStart, PeriodEnd, 1000m, "USD", "TaskReward");

        var decision1 = WorkforceIncentiveEngine.EvaluateIncentive(workforce, command);
        var decision2 = WorkforceIncentiveEngine.EvaluateIncentive(workforce, command);

        Assert.Equal(decision1.PayoutReference, decision2.PayoutReference);
    }

    private static EngineContext CreateContext(
        decimal performanceScore = 75m,
        string performanceTier = "High",
        decimal baseIncentiveAmount = 1000m,
        string currency = "USD",
        string incentiveType = "PerformanceBonus",
        string workerStatus = "Active",
        DateTimeOffset? periodStart = null,
        DateTimeOffset? periodEnd = null)
    {
        var workerId = Guid.NewGuid();

        var data = new Dictionary<string, object>
        {
            ["workforceId"] = workerId.ToString(),
            ["performanceScore"] = performanceScore,
            ["performanceTier"] = performanceTier,
            ["baseIncentiveAmount"] = baseIncentiveAmount,
            ["currency"] = currency,
            ["incentiveType"] = incentiveType,
            ["evaluationPeriodStart"] = (periodStart ?? PeriodStart).ToString("O"),
            ["evaluationPeriodEnd"] = (periodEnd ?? PeriodEnd).ToString("O"),
            ["workerName"] = "TestWorker",
            ["workerCapabilities"] = new[] { "TaxiRide" },
            ["workerStatus"] = workerStatus
        };

        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Incentive",
            "partition-1", data);
    }
}
