using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Engines.T3I.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Models;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyConflictAnalysisEngineTests
{
    private static PolicyDefinition MakePolicy(string id, string domain = "platform",
        PolicyPriority priority = PolicyPriority.Medium,
        PolicyLifecycleState lifecycle = PolicyLifecycleState.Active) =>
        new(id, $"Policy {id}", 1, domain,
            Array.Empty<PolicyCondition>(), Array.Empty<PolicyAction>(),
            DateTime.UtcNow, priority, lifecycle);

    private static PolicyConflictRecord MakeConflict(string a, string b,
        ConflictType type = ConflictType.ACTION_CONFLICT, string description = "conflict") =>
        new(a, b, type, description);

    private static PolicySimulationRecord MakeSimulation(
        string actorId, params (string policyId, bool allowed)[] decisions) =>
        new(
            new PolicyContext(Guid.NewGuid(), Guid.NewGuid(), "platform",
                new Dictionary<string, string>(), DateTime.UtcNow),
            decisions.Select(d => new PolicyDecision(d.policyId, d.allowed, "evaluate", "reason", DateTime.UtcNow)).ToList(),
            decisions.Length > 0
                ? new PolicyDecision(decisions[0].policyId, decisions[0].allowed, "evaluate", "reason", DateTime.UtcNow)
                : new PolicyDecision("none", true, "evaluate", "no decisions", DateTime.UtcNow)
        );

    [Fact]
    public void AnalyzeConflicts_ClustersRelatedConflicts()
    {
        var engine = new PolicyConflictAnalysisEngine();
        var policies = new[]
        {
            MakePolicy("p1"), MakePolicy("p2"), MakePolicy("p3")
        };
        var conflicts = new[]
        {
            MakeConflict("p1", "p2", ConflictType.ACTION_CONFLICT),
            MakeConflict("p2", "p3", ConflictType.ACTION_CONFLICT)
        };

        var result = engine.AnalyzeConflicts(new PolicyConflictAnalysisInput(
            policies, conflicts, Array.Empty<PolicySimulationRecord>()));

        Assert.NotEmpty(result.ConflictClusters);
        var cluster = result.ConflictClusters.First(c => c.ConflictType == AnalysisConflictType.ACTION_CONFLICT);
        Assert.Contains("p1", cluster.Policies);
        Assert.Contains("p2", cluster.Policies);
        Assert.Contains("p3", cluster.Policies);
        Assert.NotEmpty(cluster.ClusterId);
        Assert.NotEmpty(cluster.RecommendedAction);
    }

    [Fact]
    public void AnalyzeConflicts_DetectsPriorityOverrideChains()
    {
        var engine = new PolicyConflictAnalysisEngine();
        var policies = new[]
        {
            MakePolicy("p1", priority: PolicyPriority.Critical),
            MakePolicy("p2", priority: PolicyPriority.High),
            MakePolicy("p3", priority: PolicyPriority.Low)
        };
        var conflicts = new[]
        {
            MakeConflict("p1", "p2", ConflictType.PRIORITY_CONFLICT),
            MakeConflict("p2", "p3", ConflictType.ACTION_CONFLICT)
        };

        var result = engine.AnalyzeConflicts(new PolicyConflictAnalysisInput(
            policies, conflicts, Array.Empty<PolicySimulationRecord>()));

        Assert.NotEmpty(result.PriorityOverrideChains);
        var chain = result.PriorityOverrideChains[0];
        Assert.True(chain.PolicyChain.Count >= 2);
        Assert.NotEmpty(chain.Description);
    }

    [Fact]
    public void AnalyzeConflicts_DetectsEscalationRisks()
    {
        var engine = new PolicyConflictAnalysisEngine();
        var policies = new[]
        {
            MakePolicy("p1"), MakePolicy("p2"), MakePolicy("p3"), MakePolicy("p4")
        };
        var conflicts = new[]
        {
            MakeConflict("p1", "p2"),
            MakeConflict("p1", "p3"),
            MakeConflict("p1", "p4")
        };

        var result = engine.AnalyzeConflicts(new PolicyConflictAnalysisInput(
            policies, conflicts, Array.Empty<PolicySimulationRecord>()));

        Assert.NotEmpty(result.EscalationRisks);
        var risk = result.EscalationRisks.First(r => r.PolicyId == "p1");
        Assert.Equal("MULTI_CONFLICT_ESCALATION", risk.RiskType);
        Assert.True(risk.Severity >= ConflictSeverity.HIGH);
    }

    [Fact]
    public void AnalyzeConflicts_DeterministicOutput_SameInputSameResult()
    {
        var engine = new PolicyConflictAnalysisEngine();
        var policies = new[]
        {
            MakePolicy("z"), MakePolicy("m"), MakePolicy("a")
        };
        var conflicts = new[]
        {
            MakeConflict("z", "m"),
            MakeConflict("m", "a"),
            MakeConflict("z", "a")
        };
        var input = new PolicyConflictAnalysisInput(
            policies, conflicts, Array.Empty<PolicySimulationRecord>());

        var result1 = engine.AnalyzeConflicts(input);
        var result2 = engine.AnalyzeConflicts(input);

        Assert.Equal(result1.ConflictClusters.Count, result2.ConflictClusters.Count);
        for (var i = 0; i < result1.ConflictClusters.Count; i++)
        {
            Assert.Equal(result1.ConflictClusters[i].ClusterId, result2.ConflictClusters[i].ClusterId);
            Assert.Equal(result1.ConflictClusters[i].Policies, result2.ConflictClusters[i].Policies);
            Assert.Equal(result1.ConflictClusters[i].ConflictType, result2.ConflictClusters[i].ConflictType);
            Assert.Equal(result1.ConflictClusters[i].Severity, result2.ConflictClusters[i].Severity);
        }

        Assert.Equal(result1.EscalationRisks.Count, result2.EscalationRisks.Count);
        Assert.Equal(result1.PriorityOverrideChains.Count, result2.PriorityOverrideChains.Count);
    }

    [Fact]
    public void AnalyzeConflicts_MultiDomainConflictDetection()
    {
        var engine = new PolicyConflictAnalysisEngine();
        var policies = new[]
        {
            MakePolicy("p1", domain: "finance"),
            MakePolicy("p2", domain: "hr"),
            MakePolicy("p3", domain: "finance"),
            MakePolicy("p4", domain: "hr")
        };
        var conflicts = new[]
        {
            MakeConflict("p1", "p2"),
            MakeConflict("p3", "p4")
        };

        var result = engine.AnalyzeConflicts(new PolicyConflictAnalysisInput(
            policies, conflicts, Array.Empty<PolicySimulationRecord>()));

        Assert.Contains(result.ConflictClusters, c => c.ConflictType == AnalysisConflictType.DOMAIN_CONFLICT);
    }

    [Fact]
    public void AnalyzeConflicts_SimulationIntegration_DetectsDenialPatterns()
    {
        var engine = new PolicyConflictAnalysisEngine();
        var policies = new[]
        {
            MakePolicy("p1"), MakePolicy("p2")
        };
        var conflicts = Array.Empty<PolicyConflictRecord>();
        var simulations = new[]
        {
            MakeSimulation("actor-1", ("p1", false), ("p2", true)),
            MakeSimulation("actor-2", ("p1", false), ("p2", true)),
            MakeSimulation("actor-3", ("p1", false), ("p2", true))
        };

        var result = engine.AnalyzeConflicts(new PolicyConflictAnalysisInput(
            policies, conflicts, simulations));

        Assert.NotEmpty(result.EscalationRisks);
        var risk = result.EscalationRisks.First(r => r.PolicyId == "p1");
        Assert.Equal("SIMULATION_DENIAL_PATTERN", risk.RiskType);
    }

    [Fact]
    public void AnalyzeConflicts_ConcurrentExecutionSafety()
    {
        var engine = new PolicyConflictAnalysisEngine();
        var policies = new[]
        {
            MakePolicy("p1"), MakePolicy("p2"), MakePolicy("p3")
        };
        var conflicts = new[]
        {
            MakeConflict("p1", "p2"),
            MakeConflict("p2", "p3")
        };
        var input = new PolicyConflictAnalysisInput(
            policies, conflicts, Array.Empty<PolicySimulationRecord>());

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => engine.AnalyzeConflicts(input)))
            .ToArray();

        Task.WaitAll(tasks);

        var expected = tasks[0].Result.ConflictClusters.Select(c => c.ClusterId).ToList();
        foreach (var task in tasks)
        {
            var actual = task.Result.ConflictClusters.Select(c => c.ClusterId).ToList();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void AnalyzeConflicts_EmptyInput_ReturnsEmptyResult()
    {
        var engine = new PolicyConflictAnalysisEngine();
        var input = new PolicyConflictAnalysisInput(
            Array.Empty<PolicyDefinition>(),
            Array.Empty<PolicyConflictRecord>(),
            Array.Empty<PolicySimulationRecord>());

        var result = engine.AnalyzeConflicts(input);

        Assert.Empty(result.ConflictClusters);
        Assert.Empty(result.EscalationRisks);
        Assert.Empty(result.PriorityOverrideChains);
    }
}
