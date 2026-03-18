using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Simulation.Engines;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicySimulationTests
{
    private readonly PolicyRegistryStore _registryStore = new();
    private readonly PolicyDependencyStore _dependencyStore = new();
    private readonly PolicyDecisionCacheStore _cacheStore = new();
    private readonly PolicySimulationEngine _engine;

    public PolicySimulationTests()
    {
        _engine = new PolicySimulationEngine(_registryStore, _dependencyStore);
    }

    private void RegisterPolicy(string id, string domain, List<PolicyCondition> conditions, List<PolicyAction> actions)
    {
        var definition = new PolicyDefinition(id, $"Policy {id}", 1, domain, conditions, actions, DateTime.UtcNow);
        var record = new PolicyRecord(id, 1, definition, PolicyStatus.Active, DateTime.UtcNow);
        _registryStore.Register(record);
    }

    private static PolicySimulationRequest CreateRequest(string domain, string actorId, Dictionary<string, string> attributes)
    {
        return new PolicySimulationRequest(domain, actorId, attributes);
    }

    [Fact]
    public void Simulate_AllowPolicy_ReturnsAllowed()
    {
        RegisterPolicy("sim-allow", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var request = CreateRequest("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "75" });

        var result = _engine.SimulatePolicyEvaluation(request);

        Assert.Single(result.Decisions);
        Assert.True(result.Decisions[0].Allowed);
        Assert.Equal("allow", result.Decisions[0].Action);
    }

    [Fact]
    public void Simulate_DenyPolicy_ReturnsDenied()
    {
        RegisterPolicy("sim-deny", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "50") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var request = CreateRequest("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "30" });

        var result = _engine.SimulatePolicyEvaluation(request);

        Assert.Single(result.Decisions);
        Assert.False(result.Decisions[0].Allowed);
        Assert.Equal("deny", result.Decisions[0].Action);
    }

    [Fact]
    public void Simulate_MultiplePolicies_ReturnsMultipleDecisions()
    {
        RegisterPolicy("sim-multi-1", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy("sim-multi-2", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "100") },
            new List<PolicyAction> { new("log", new Dictionary<string, string>()) });

        var request = CreateRequest("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "75" });

        var result = _engine.SimulatePolicyEvaluation(request);

        Assert.Equal(2, result.Decisions.Count);
    }

    [Fact]
    public void Simulate_DoesNotModifyRegistry()
    {
        RegisterPolicy("sim-reg", "identity",
            new List<PolicyCondition>(),
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var countBefore = _registryStore.GetAll().Count;

        var request = CreateRequest("identity", Guid.NewGuid().ToString(), new Dictionary<string, string>());
        _engine.SimulatePolicyEvaluation(request);

        var countAfter = _registryStore.GetAll().Count;
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public void Simulate_DoesNotModifyCache()
    {
        RegisterPolicy("sim-cache", "identity",
            new List<PolicyCondition>(),
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var request = CreateRequest("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "75" });
        _engine.SimulatePolicyEvaluation(request);

        Assert.Empty(_cacheStore.GetAll());
    }

    [Fact]
    public void Simulate_ResultsMatchEvaluationEngine()
    {
        RegisterPolicy("sim-match", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var actorId = Guid.NewGuid();
        var attrs = new Dictionary<string, string> { ["trust_score"] = "75" };

        var evalEngine = new PolicyEvaluationEngine(_registryStore, _dependencyStore);
        var context = new PolicyContext(Guid.NewGuid(), actorId, "identity", attrs, DateTime.UtcNow);
        var evalDecisions = evalEngine.EvaluatePolicies("identity", context);

        var request = CreateRequest("identity", actorId.ToString(), attrs);
        var simResult = _engine.SimulatePolicyEvaluation(request);

        Assert.Equal(evalDecisions.Count, simResult.Decisions.Count);
        for (var i = 0; i < evalDecisions.Count; i++)
        {
            Assert.Equal(evalDecisions[i].PolicyId, simResult.Decisions[i].PolicyId);
            Assert.Equal(evalDecisions[i].Allowed, simResult.Decisions[i].Allowed);
            Assert.Equal(evalDecisions[i].Action, simResult.Decisions[i].Action);
        }
    }

    [Fact]
    public void Simulate_TimestampRecorded()
    {
        var before = DateTime.UtcNow;

        RegisterPolicy("sim-ts", "identity",
            new List<PolicyCondition>(),
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var request = CreateRequest("identity", Guid.NewGuid().ToString(), new Dictionary<string, string>());
        var result = _engine.SimulatePolicyEvaluation(request);

        Assert.True(result.SimulatedAt >= before);
        Assert.True(result.SimulatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Simulate_EmptyPolicySet_ReturnsEmptyDecisions()
    {
        var request = CreateRequest("nonexistent-domain", Guid.NewGuid().ToString(), new Dictionary<string, string>());

        var result = _engine.SimulatePolicyEvaluation(request);

        Assert.Empty(result.Decisions);
        Assert.Equal("nonexistent-domain", result.Domain);
    }
}
