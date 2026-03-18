namespace Whycespace.DecisionEngines.Tests;

using Whycespace.Engines.T3I.Atlas.Workforce.Engines;
using Whycespace.Engines.T3I.Atlas.Workforce.Models;
using Whycespace.Domain.Clusters.Operations.Shared;
using Whycespace.Contracts.Engines;

public sealed class WorkforceComplianceEngineTests
{
    private readonly WorkforceComplianceEngine _engine = new();

    private static readonly DateTimeOffset PeriodStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 3, 31, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset RecentReview = new(2026, 2, 15, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FullyCompliantWorker_ReturnsCompliant()
    {
        var context = CreateContext(
            workerStatus: "Active",
            capabilities: new[] { "TaxiRide", "Inspection" },
            workerCapabilities: new[] { "TaxiRide", "Inspection" },
            completedTasks: 90,
            failedTasks: 5,
            lastPolicyReviewDate: RecentReview);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["compliant"]);
        Assert.Equal(100m, result.Output["complianceScore"]);
    }

    [Fact]
    public async Task SuspendedWorker_ReturnsViolation()
    {
        var context = CreateContext(
            workerStatus: "Suspended",
            capabilities: new[] { "TaxiRide" },
            workerCapabilities: new[] { "TaxiRide" },
            completedTasks: 50,
            failedTasks: 5,
            lastPolicyReviewDate: RecentReview);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["compliant"]);
        var violations = result.Output["violations"] as IReadOnlyList<string>;
        Assert.NotNull(violations);
        Assert.Contains("SuspendedWorkerActivity", violations);
    }

    [Fact]
    public async Task CapabilityMismatch_DetectsViolation()
    {
        var context = CreateContext(
            workerStatus: "Active",
            capabilities: new[] { "TaxiRide", "PropertyMaintenance", "Inspection" },
            workerCapabilities: new[] { "TaxiRide" },
            completedTasks: 50,
            failedTasks: 5,
            lastPolicyReviewDate: RecentReview);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["compliant"]);
        var violations = result.Output["violations"] as IReadOnlyList<string>;
        Assert.NotNull(violations);
        Assert.Contains("CapabilityMismatch", violations);
    }

    [Fact]
    public async Task ExcessiveFailures_DetectsViolation()
    {
        var context = CreateContext(
            workerStatus: "Active",
            capabilities: new[] { "TaxiRide" },
            workerCapabilities: new[] { "TaxiRide" },
            completedTasks: 30,
            failedTasks: 20,
            lastPolicyReviewDate: RecentReview);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["compliant"]);
        var violations = result.Output["violations"] as IReadOnlyList<string>;
        Assert.NotNull(violations);
        Assert.Contains("ExcessiveFailures", violations);
    }

    [Fact]
    public async Task InvalidCompliancePeriod_ReturnsNonCompliant()
    {
        var context = CreateContext(
            workerStatus: "Active",
            capabilities: new[] { "TaxiRide" },
            workerCapabilities: new[] { "TaxiRide" },
            completedTasks: 50,
            failedTasks: 5,
            lastPolicyReviewDate: RecentReview,
            periodStart: PeriodEnd,
            periodEnd: PeriodStart);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["compliant"]);
        Assert.Equal(0m, result.Output["complianceScore"]);
    }

    [Fact]
    public async Task MissingPolicyReview_DetectsViolation()
    {
        var context = CreateContext(
            workerStatus: "Active",
            capabilities: new[] { "TaxiRide" },
            workerCapabilities: new[] { "TaxiRide" },
            completedTasks: 50,
            failedTasks: 5,
            lastPolicyReviewDate: null);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["compliant"]);
        var violations = result.Output["violations"] as IReadOnlyList<string>;
        Assert.NotNull(violations);
        Assert.Contains("PolicyViolation", violations);
    }

    [Fact]
    public async Task MissingRequiredFields_ReturnsFailure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Compliance",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void EvaluateCompliance_Directly_FullyCompliant()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "GoodWorker", new[] { "TaxiRide", "Inspection" });

        var command = new WorkforceComplianceCommand(
            workforce.WorkerId, "Active",
            new[] { "TaxiRide", "Inspection" },
            CompletedTasks: 80, FailedTasks: 5,
            PeriodStart, PeriodEnd, RecentReview);

        var decision = WorkforceComplianceEngine.EvaluateCompliance(workforce, command);

        Assert.True(decision.Compliant);
        Assert.Equal(100m, decision.ComplianceScore);
        Assert.Empty(decision.Violations);
    }

    [Fact]
    public void EvaluateCompliance_Directly_MultipleViolations()
    {
        var workforce = WorkforceAggregate.Register(
            WorkerId.New(), "BadWorker", new[] { "TaxiRide" });
        workforce.Suspend();

        var command = new WorkforceComplianceCommand(
            workforce.WorkerId, "Suspended",
            new[] { "TaxiRide", "PropertyMaintenance" },
            CompletedTasks: 10, FailedTasks: 15,
            PeriodStart, PeriodEnd, null);

        var decision = WorkforceComplianceEngine.EvaluateCompliance(workforce, command);

        Assert.False(decision.Compliant);
        Assert.True(decision.ComplianceScore < 50m);
        Assert.Contains("SuspendedWorkerActivity", decision.Violations);
        Assert.Contains("CapabilityMismatch", decision.Violations);
        Assert.Contains("ExcessiveFailures", decision.Violations);
        Assert.Contains("PolicyViolation", decision.Violations);
    }

    private static EngineContext CreateContext(
        string workerStatus = "Active",
        string[]? capabilities = null,
        string[]? workerCapabilities = null,
        int completedTasks = 50,
        int failedTasks = 5,
        DateTimeOffset? lastPolicyReviewDate = null,
        DateTimeOffset? periodStart = null,
        DateTimeOffset? periodEnd = null)
    {
        var workerId = Guid.NewGuid();

        var data = new Dictionary<string, object>
        {
            ["workforceId"] = workerId.ToString(),
            ["workerStatus"] = workerStatus,
            ["capabilities"] = capabilities ?? new[] { "TaxiRide" },
            ["workerCapabilities"] = workerCapabilities ?? new[] { "TaxiRide" },
            ["completedTasks"] = completedTasks,
            ["failedTasks"] = failedTasks,
            ["compliancePeriodStart"] = (periodStart ?? PeriodStart).ToString("O"),
            ["compliancePeriodEnd"] = (periodEnd ?? PeriodEnd).ToString("O"),
            ["workerName"] = "TestWorker"
        };

        if (lastPolicyReviewDate is not null)
            data["lastPolicyReviewDate"] = lastPolicyReviewDate.Value.ToString("O");

        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Compliance",
            "partition-1", data);
    }
}
