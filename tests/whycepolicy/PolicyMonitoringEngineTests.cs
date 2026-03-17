using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Engines.T3I.Monitoring.Policy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using PolicyMonitoringEngine = Whycespace.Engines.T3I.WhycePolicy.PolicyMonitoringEngine;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyMonitoringEngineTests
{
    private static readonly TimeRange DefaultWindow = new(
        new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc));

    private static PolicyDecision MakeDecision(string policyId, bool allowed,
        string action = "evaluate") =>
        new(policyId, allowed, action, allowed ? "passed" : "denied", DateTime.UtcNow);

    private static PolicyEvaluationResult MakeEvalResult(params PolicyDecision[] decisions) =>
        new(decisions.ToList(), decisions.Length > 0 ? decisions[0] : MakeDecision("none", true));

    private static PolicyEnforcementResult MakeEnforcementResult(bool allowed, params PolicyDecision[] decisions) =>
        new(allowed, allowed ? "All policies passed" : "Policy denied", decisions.ToList(), DateTime.UtcNow);

    [Fact]
    public void AnalyzeMonitoringWindow_CalculatesStatisticsCorrectly()
    {
        var engine = new PolicyMonitoringEngine();
        var decisions = new[]
        {
            MakeEvalResult(MakeDecision("p1", true), MakeDecision("p2", false)),
            MakeEvalResult(MakeDecision("p1", true), MakeDecision("p2", true)),
            MakeEvalResult(MakeDecision("p1", false), MakeDecision("p2", false))
        };

        var input = new PolicyMonitoringInput(decisions, Array.Empty<PolicyEnforcementResult>(), DefaultWindow);
        var result = engine.AnalyzeMonitoringWindow(input);

        Assert.Equal(6, result.DecisionStatistics.TotalEvaluations);
        Assert.Equal(3, result.DecisionStatistics.TotalDenials);
        Assert.Equal(3, result.DecisionStatistics.TotalAllowed);
        Assert.Equal(0.5, result.DecisionStatistics.DenialRate);
    }

    [Fact]
    public void AnalyzeMonitoringWindow_DetectsExcessiveDenialRate()
    {
        var engine = new PolicyMonitoringEngine();
        var decisions = new[]
        {
            MakeEvalResult(MakeDecision("p1", false)),
            MakeEvalResult(MakeDecision("p1", false)),
            MakeEvalResult(MakeDecision("p1", false)),
            MakeEvalResult(MakeDecision("p1", true))
        };

        var input = new PolicyMonitoringInput(decisions, Array.Empty<PolicyEnforcementResult>(), DefaultWindow);
        var result = engine.AnalyzeMonitoringWindow(input);

        Assert.Contains(result.AnomalyRecords, a =>
            a.PolicyId == "p1" &&
            a.AnomalyType == PolicyAnomalyType.EXCESSIVE_DENIAL_RATE &&
            a.Severity >= PolicyAnomalySeverity.HIGH);
    }

    [Fact]
    public void AnalyzeMonitoringWindow_DetectsEscalationAnomaly()
    {
        var engine = new PolicyMonitoringEngine();
        var decisions = new[]
        {
            MakeEvalResult(MakeDecision("p1", true, "escalate")),
            MakeEvalResult(MakeDecision("p1", true, "escalate")),
            MakeEvalResult(MakeDecision("p1", true, "allow"))
        };

        var input = new PolicyMonitoringInput(decisions, Array.Empty<PolicyEnforcementResult>(), DefaultWindow);
        var result = engine.AnalyzeMonitoringWindow(input);

        Assert.Contains(result.AnomalyRecords, a =>
            a.PolicyId == "p1" &&
            a.AnomalyType == PolicyAnomalyType.FREQUENT_ESCALATION);
    }

    [Fact]
    public void AnalyzeMonitoringWindow_DetectsConflictSpike()
    {
        var engine = new PolicyMonitoringEngine();
        var decisions = new[]
        {
            MakeEvalResult(MakeDecision("p1", false), MakeDecision("p2", true)),
            MakeEvalResult(MakeDecision("p1", false), MakeDecision("p2", true)),
            MakeEvalResult(MakeDecision("p1", false), MakeDecision("p2", true))
        };

        var input = new PolicyMonitoringInput(decisions, Array.Empty<PolicyEnforcementResult>(), DefaultWindow);
        var result = engine.AnalyzeMonitoringWindow(input);

        Assert.Contains(result.AnomalyRecords, a =>
            a.PolicyId == "p1" &&
            a.AnomalyType == PolicyAnomalyType.CONFLICT_SPIKE);
    }

    [Fact]
    public void AnalyzeMonitoringWindow_DeterministicOutput()
    {
        var engine = new PolicyMonitoringEngine();
        var decisions = new[]
        {
            MakeEvalResult(MakeDecision("z1", false), MakeDecision("a1", true)),
            MakeEvalResult(MakeDecision("z1", false), MakeDecision("a1", false)),
            MakeEvalResult(MakeDecision("z1", false), MakeDecision("a1", true)),
            MakeEvalResult(MakeDecision("z1", false), MakeDecision("a1", true))
        };

        var input = new PolicyMonitoringInput(decisions, Array.Empty<PolicyEnforcementResult>(), DefaultWindow);

        var result1 = engine.AnalyzeMonitoringWindow(input);
        var result2 = engine.AnalyzeMonitoringWindow(input);

        Assert.Equal(result1.DecisionStatistics.TotalEvaluations, result2.DecisionStatistics.TotalEvaluations);
        Assert.Equal(result1.DecisionStatistics.DenialRate, result2.DecisionStatistics.DenialRate);
        Assert.Equal(result1.AnomalyRecords.Count, result2.AnomalyRecords.Count);
        for (var i = 0; i < result1.AnomalyRecords.Count; i++)
        {
            Assert.Equal(result1.AnomalyRecords[i].PolicyId, result2.AnomalyRecords[i].PolicyId);
            Assert.Equal(result1.AnomalyRecords[i].AnomalyType, result2.AnomalyRecords[i].AnomalyType);
            Assert.Equal(result1.AnomalyRecords[i].Severity, result2.AnomalyRecords[i].Severity);
        }
    }

    [Fact]
    public void AnalyzeMonitoringWindow_EmptyInput_ReturnsEmptyReport()
    {
        var engine = new PolicyMonitoringEngine();
        var input = new PolicyMonitoringInput(
            Array.Empty<PolicyEvaluationResult>(),
            Array.Empty<PolicyEnforcementResult>(),
            DefaultWindow);

        var result = engine.AnalyzeMonitoringWindow(input);

        Assert.Equal(0, result.DecisionStatistics.TotalEvaluations);
        Assert.Equal(0, result.DecisionStatistics.TotalDenials);
        Assert.Equal(0.0, result.DecisionStatistics.DenialRate);
        Assert.Empty(result.AnomalyRecords);
    }

    [Fact]
    public void AnalyzeMonitoringWindow_ConcurrentExecutionSafety()
    {
        var engine = new PolicyMonitoringEngine();
        var decisions = new[]
        {
            MakeEvalResult(MakeDecision("p1", false), MakeDecision("p2", true)),
            MakeEvalResult(MakeDecision("p1", false), MakeDecision("p2", true)),
            MakeEvalResult(MakeDecision("p1", false), MakeDecision("p2", true))
        };

        var input = new PolicyMonitoringInput(decisions, Array.Empty<PolicyEnforcementResult>(), DefaultWindow);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => engine.AnalyzeMonitoringWindow(input)))
            .ToArray();

        Task.WaitAll(tasks);

        var expectedAnomalyCount = tasks[0].Result.AnomalyRecords.Count;
        var expectedStats = tasks[0].Result.DecisionStatistics;

        foreach (var task in tasks)
        {
            Assert.Equal(expectedAnomalyCount, task.Result.AnomalyRecords.Count);
            Assert.Equal(expectedStats.TotalEvaluations, task.Result.DecisionStatistics.TotalEvaluations);
            Assert.Equal(expectedStats.DenialRate, task.Result.DecisionStatistics.DenialRate);
        }
    }
}
