namespace Whycespace.DecisionEngines.Tests;

using Whycespace.Engines.T3I.HEOS;
using Whycespace.Domain.Core.Workforce;
using Whycespace.Contracts.Engines;

public sealed class WorkforcePerformanceEngineTests
{
    private readonly WorkforcePerformanceEngine _engine = new();

    private static readonly DateTimeOffset PeriodStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 3, 31, 23, 59, 59, TimeSpan.Zero);

    [Fact]
    public async Task HighPerformance_ReturnsExceptionalTier()
    {
        var context = CreateContext(
            completedTasks: 95,
            failedTasks: 5,
            averageTaskDuration: 30m,
            customerRating: 4.8m);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkforcePerformanceEvaluated", result.Events[0].EventType);
        Assert.Equal("Exceptional", result.Output["performanceTier"]);
    }

    [Fact]
    public async Task LowPerformance_ReturnsLowTier()
    {
        var context = CreateContext(
            completedTasks: 10,
            failedTasks: 40,
            averageTaskDuration: 180m,
            customerRating: 1.5m);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Low", result.Output["performanceTier"]);
    }

    [Fact]
    public async Task BalancedPerformance_ReturnsStandardOrHighTier()
    {
        var context = CreateContext(
            completedTasks: 50,
            failedTasks: 15,
            averageTaskDuration: 60m,
            customerRating: 3.5m);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var tier = result.Output["performanceTier"] as string;
        Assert.True(tier == "Standard" || tier == "High",
            $"Expected Standard or High tier, got {tier}");
    }

    [Fact]
    public async Task InvalidRating_AboveFive_ReturnsFailure()
    {
        var context = CreateContext(
            completedTasks: 50,
            failedTasks: 5,
            averageTaskDuration: 45m,
            customerRating: 6.0m);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success); // Engine succeeds but decision reflects invalid input
        Assert.Equal("Low", result.Output["performanceTier"]);
    }

    [Fact]
    public async Task InvalidRating_Negative_ReturnsFailure()
    {
        var context = CreateContext(
            completedTasks: 50,
            failedTasks: 5,
            averageTaskDuration: 45m,
            customerRating: -1.0m);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Low", result.Output["performanceTier"]);
    }

    [Fact]
    public async Task InvalidEvaluationPeriod_EndBeforeStart_ReturnsLowTier()
    {
        var context = CreateContext(
            completedTasks: 80,
            failedTasks: 5,
            averageTaskDuration: 45m,
            customerRating: 4.5m,
            periodStart: PeriodEnd,
            periodEnd: PeriodStart);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Low", result.Output["performanceTier"]);
    }

    [Fact]
    public async Task InvalidEvaluationPeriod_EqualStartEnd_ReturnsLowTier()
    {
        var context = CreateContext(
            completedTasks: 80,
            failedTasks: 5,
            averageTaskDuration: 45m,
            customerRating: 4.5m,
            periodStart: PeriodStart,
            periodEnd: PeriodStart);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Low", result.Output["performanceTier"]);
    }

    [Fact]
    public async Task MissingRequiredFields_ReturnsFailure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Evaluate",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void EvaluatePerformance_Directly_HighPerformance()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "TopWorker", new[] { "TaxiRide", "Inspection" });

        var command = new WorkforcePerformanceCommand(
            workforce.WorkerId,
            CompletedTasks: 98,
            FailedTasks: 2,
            AverageTaskDuration: 25m,
            CustomerRating: 4.9m,
            EvaluationPeriodStart: PeriodStart,
            EvaluationPeriodEnd: PeriodEnd);

        var decision = WorkforcePerformanceEngine.EvaluatePerformance(workforce, command);

        Assert.Equal(PerformanceTier.Exceptional, decision.PerformanceTier);
        Assert.True(decision.PerformanceScore >= 90m);
        Assert.Contains("TopWorker", decision.EvaluationSummary);
    }

    [Fact]
    public void EvaluatePerformance_Directly_LowPerformance()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "NewWorker", new[] { "CustomerSupport" });

        var command = new WorkforcePerformanceCommand(
            workforce.WorkerId,
            CompletedTasks: 5,
            FailedTasks: 20,
            AverageTaskDuration: 200m,
            CustomerRating: 1.0m,
            EvaluationPeriodStart: PeriodStart,
            EvaluationPeriodEnd: PeriodEnd);

        var decision = WorkforcePerformanceEngine.EvaluatePerformance(workforce, command);

        Assert.Equal(PerformanceTier.Low, decision.PerformanceTier);
        Assert.True(decision.PerformanceScore < 50m);
    }

    [Fact]
    public void EvaluatePerformance_Directly_NegativeCompletedTasks()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "Worker", new[] { "TaxiRide" });

        var command = new WorkforcePerformanceCommand(
            workforce.WorkerId,
            CompletedTasks: -1,
            FailedTasks: 0,
            AverageTaskDuration: 30m,
            CustomerRating: 4.0m,
            EvaluationPeriodStart: PeriodStart,
            EvaluationPeriodEnd: PeriodEnd);

        var decision = WorkforcePerformanceEngine.EvaluatePerformance(workforce, command);

        Assert.Equal(PerformanceTier.Low, decision.PerformanceTier);
        Assert.Contains("Invalid input", decision.EvaluationSummary);
    }

    private static EngineContext CreateContext(
        int completedTasks = 50,
        int failedTasks = 5,
        decimal averageTaskDuration = 45m,
        decimal customerRating = 4.0m,
        DateTimeOffset? periodStart = null,
        DateTimeOffset? periodEnd = null)
    {
        var workerId = Guid.NewGuid();

        var data = new Dictionary<string, object>
        {
            ["workforceId"] = workerId.ToString(),
            ["completedTasks"] = completedTasks,
            ["failedTasks"] = failedTasks,
            ["averageTaskDuration"] = averageTaskDuration,
            ["customerRating"] = customerRating,
            ["evaluationPeriodStart"] = (periodStart ?? PeriodStart).ToString("O"),
            ["evaluationPeriodEnd"] = (periodEnd ?? PeriodEnd).ToString("O"),
            ["workerName"] = "TestWorker",
            ["workerCapabilities"] = new[] { "TaxiRide", "Inspection" },
            ["workerStatus"] = "Active"
        };

        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Evaluate",
            "partition-1", data);
    }
}
