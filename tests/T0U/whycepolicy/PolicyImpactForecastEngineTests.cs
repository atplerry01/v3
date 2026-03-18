using Whycespace.Engines.T3I.Forecasting.Policy.Engines;
using Whycespace.Engines.T3I.Forecasting.Policy.Models;
using Whycespace.Engines.T3I.Shared;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyImpactForecastEngineTests
{
    private static PolicyDefinition MakePolicy(
        string id,
        string domain,
        IReadOnlyList<PolicyCondition>? conditions = null,
        IReadOnlyList<PolicyAction>? actions = null) =>
        new(id, $"Policy {id}", 1, domain,
            conditions ?? Array.Empty<PolicyCondition>(),
            actions ?? Array.Empty<PolicyAction>(),
            DateTime.UtcNow);

    private static PolicyContext MakeContext(
        Guid contextId,
        string domain,
        Dictionary<string, string>? attributes = null) =>
        new(contextId, Guid.NewGuid(), domain,
            attributes ?? new Dictionary<string, string>(),
            DateTime.UtcNow);

    private static PolicyImpactForecastResult ExecuteForecast(
        PolicyImpactForecastEngine engine,
        PolicyImpactForecastInput input)
    {
        var context = IntelligenceContext<PolicyImpactForecastInput>.Create(input);
        var result = engine.Execute(context);
        Assert.True(result.Success);
        Assert.NotNull(result.Output);
        return result.Output;
    }

    [Fact]
    public void ForecastImpact_NoChanges_ReturnsNoImpact()
    {
        var engine = new PolicyImpactForecastEngine();
        var policies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) })
        };
        var contextId = Guid.NewGuid();
        var context = MakeContext(contextId, "finance");

        var input = new PolicyImpactForecastInput(policies, policies, new[] { context });
        var result = ExecuteForecast(engine, input);

        Assert.Single(result.ForecastRecords);
        Assert.Equal(ImpactType.NO_CHANGE, result.ForecastRecords[0].ImpactType);
        Assert.Equal(ImpactSeverity.LOW, result.ForecastRecords[0].Severity);
        Assert.Empty(result.AffectedPolicies);
        Assert.Equal(ImpactSeverity.LOW, result.RiskLevel);
    }

    [Fact]
    public void ForecastImpact_DecisionChange_DetectsChange()
    {
        var engine = new PolicyImpactForecastEngine();
        var contextId = Guid.NewGuid();
        var context = MakeContext(contextId, "finance");

        var currentPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) })
        };
        var proposedPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("deny", new Dictionary<string, string>()) })
        };

        var input = new PolicyImpactForecastInput(currentPolicies, proposedPolicies, new[] { context });
        var result = ExecuteForecast(engine, input);

        Assert.Single(result.ForecastRecords);
        Assert.NotEqual(ImpactType.NO_CHANGE, result.ForecastRecords[0].ImpactType);
        Assert.Equal("allow", result.ForecastRecords[0].CurrentDecision);
        Assert.Equal("deny", result.ForecastRecords[0].ProposedDecision);
        Assert.Contains("p1", result.AffectedPolicies);
        Assert.Equal(ImpactSeverity.HIGH, result.ForecastRecords[0].Severity);
    }

    [Fact]
    public void ForecastImpact_EscalationChange_DetectsEscalation()
    {
        var engine = new PolicyImpactForecastEngine();
        var contextId = Guid.NewGuid();
        var context = MakeContext(contextId, "finance");

        var currentPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) })
        };
        var proposedPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("escalate", new Dictionary<string, string>()) })
        };

        var input = new PolicyImpactForecastInput(currentPolicies, proposedPolicies, new[] { context });
        var result = ExecuteForecast(engine, input);

        Assert.Single(result.ForecastRecords);
        Assert.Equal(ImpactType.ESCALATION_CHANGE, result.ForecastRecords[0].ImpactType);
        Assert.Equal("allow", result.ForecastRecords[0].CurrentDecision);
        Assert.Equal("escalate", result.ForecastRecords[0].ProposedDecision);
    }

    [Fact]
    public void ForecastImpact_GovernanceRuleChange_DetectsGovernanceImpact()
    {
        var engine = new PolicyImpactForecastEngine();
        var contextId = Guid.NewGuid();
        var context = MakeContext(contextId, "finance");

        var currentPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) })
        };
        var proposedPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("governance_review", new Dictionary<string, string>()) })
        };

        var input = new PolicyImpactForecastInput(currentPolicies, proposedPolicies, new[] { context });
        var result = ExecuteForecast(engine, input);

        Assert.Single(result.ForecastRecords);
        Assert.Equal(ImpactType.GOVERNANCE_CHANGE, result.ForecastRecords[0].ImpactType);
        Assert.Equal(ImpactSeverity.HIGH, result.ForecastRecords[0].Severity);
    }

    [Fact]
    public void ForecastImpact_DeterministicResults_SameInputProducesSameOutput()
    {
        var engine = new PolicyImpactForecastEngine();
        var contextId = Guid.NewGuid();
        var contexts = new[]
        {
            MakeContext(contextId, "finance"),
            MakeContext(Guid.NewGuid(), "hr")
        };
        var currentPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) }),
            MakePolicy("p2", "hr",
                actions: new[] { new PolicyAction("deny", new Dictionary<string, string>()) })
        };
        var proposedPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("deny", new Dictionary<string, string>()) }),
            MakePolicy("p2", "hr",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) })
        };

        var input = new PolicyImpactForecastInput(currentPolicies, proposedPolicies, contexts);
        var result1 = ExecuteForecast(engine, input);
        var result2 = ExecuteForecast(engine, input);

        Assert.Equal(result1.ForecastRecords.Count, result2.ForecastRecords.Count);
        for (var i = 0; i < result1.ForecastRecords.Count; i++)
        {
            Assert.Equal(result1.ForecastRecords[i].ContextId, result2.ForecastRecords[i].ContextId);
            Assert.Equal(result1.ForecastRecords[i].CurrentDecision, result2.ForecastRecords[i].CurrentDecision);
            Assert.Equal(result1.ForecastRecords[i].ProposedDecision, result2.ForecastRecords[i].ProposedDecision);
            Assert.Equal(result1.ForecastRecords[i].ImpactType, result2.ForecastRecords[i].ImpactType);
            Assert.Equal(result1.ForecastRecords[i].Severity, result2.ForecastRecords[i].Severity);
        }
        Assert.Equal(result1.AffectedPolicies, result2.AffectedPolicies);
        Assert.Equal(result1.RiskLevel, result2.RiskLevel);
    }

    [Fact]
    public void ForecastImpact_MultipleContexts_EvaluatesEach()
    {
        var engine = new PolicyImpactForecastEngine();
        var contexts = new[]
        {
            MakeContext(Guid.NewGuid(), "finance"),
            MakeContext(Guid.NewGuid(), "hr"),
            MakeContext(Guid.NewGuid(), "operations")
        };
        var currentPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) }),
            MakePolicy("p2", "hr",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) })
        };
        var proposedPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("deny", new Dictionary<string, string>()) }),
            MakePolicy("p2", "hr",
                actions: new[] { new PolicyAction("escalate", new Dictionary<string, string>()) })
        };

        var input = new PolicyImpactForecastInput(currentPolicies, proposedPolicies, contexts);
        var result = ExecuteForecast(engine, input);

        Assert.Equal(3, result.ForecastRecords.Count);
        Assert.Contains(result.ForecastRecords, r => r.ImpactType != ImpactType.NO_CHANGE);
    }

    [Fact]
    public void ForecastImpact_ConcurrentSafety_ProducesConsistentResults()
    {
        var engine = new PolicyImpactForecastEngine();
        var contexts = new[]
        {
            MakeContext(Guid.NewGuid(), "finance"),
            MakeContext(Guid.NewGuid(), "hr")
        };
        var currentPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) }),
            MakePolicy("p2", "hr",
                actions: new[] { new PolicyAction("deny", new Dictionary<string, string>()) })
        };
        var proposedPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("deny", new Dictionary<string, string>()) }),
            MakePolicy("p2", "hr",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) })
        };

        var input = new PolicyImpactForecastInput(currentPolicies, proposedPolicies, contexts);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => ExecuteForecast(engine, input)))
            .ToArray();

        Task.WaitAll(tasks);

        var expected = tasks[0].Result;
        foreach (var task in tasks)
        {
            var actual = task.Result;
            Assert.Equal(expected.ForecastRecords.Count, actual.ForecastRecords.Count);
            Assert.Equal(expected.RiskLevel, actual.RiskLevel);
            Assert.Equal(expected.AffectedPolicies, actual.AffectedPolicies);
            for (var i = 0; i < expected.ForecastRecords.Count; i++)
            {
                Assert.Equal(expected.ForecastRecords[i].ImpactType, actual.ForecastRecords[i].ImpactType);
                Assert.Equal(expected.ForecastRecords[i].Severity, actual.ForecastRecords[i].Severity);
            }
        }
    }

    [Fact]
    public void ForecastImpact_ConditionMatching_EvaluatesCorrectly()
    {
        var engine = new PolicyImpactForecastEngine();
        var contextId = Guid.NewGuid();
        var context = MakeContext(contextId, "finance",
            new Dictionary<string, string> { ["role"] = "admin" });

        var currentPolicies = new[]
        {
            MakePolicy("p1", "finance",
                conditions: new[] { new PolicyCondition("role", "equals", "admin") },
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) })
        };
        var proposedPolicies = new[]
        {
            MakePolicy("p1", "finance",
                conditions: new[] { new PolicyCondition("role", "equals", "admin") },
                actions: new[] { new PolicyAction("deny", new Dictionary<string, string>()) })
        };

        var input = new PolicyImpactForecastInput(currentPolicies, proposedPolicies, new[] { context });
        var result = ExecuteForecast(engine, input);

        Assert.Single(result.ForecastRecords);
        Assert.Equal("allow", result.ForecastRecords[0].CurrentDecision);
        Assert.Equal("deny", result.ForecastRecords[0].ProposedDecision);
    }

    [Fact]
    public void ForecastImpact_DenyToAllow_ReturnsCriticalSeverity()
    {
        var engine = new PolicyImpactForecastEngine();
        var contextId = Guid.NewGuid();
        var context = MakeContext(contextId, "finance");

        var currentPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("deny", new Dictionary<string, string>()) })
        };
        var proposedPolicies = new[]
        {
            MakePolicy("p1", "finance",
                actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) })
        };

        var input = new PolicyImpactForecastInput(currentPolicies, proposedPolicies, new[] { context });
        var result = ExecuteForecast(engine, input);

        Assert.Equal(ImpactSeverity.CRITICAL, result.ForecastRecords[0].Severity);
        Assert.Equal(ImpactSeverity.CRITICAL, result.RiskLevel);
    }

    [Fact]
    public void ForecastImpact_EmptyContexts_ReturnsEmptyResult()
    {
        var engine = new PolicyImpactForecastEngine();
        var currentPolicies = new[] { MakePolicy("p1", "finance") };
        var proposedPolicies = new[] { MakePolicy("p1", "finance") };

        var input = new PolicyImpactForecastInput(currentPolicies, proposedPolicies, Array.Empty<PolicyContext>());
        var result = ExecuteForecast(engine, input);

        Assert.Empty(result.ForecastRecords);
        Assert.Empty(result.AffectedPolicies);
        Assert.Equal(ImpactSeverity.LOW, result.RiskLevel);
    }
}
