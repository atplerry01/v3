using Whycespace.Engines.T0U.WhycePolicy.Simulation.Engines;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Tests;

public class PolicySimulationEngineTests
{
    private static PolicyDefinition MakePolicy(string id, string action = "allow", string? conditionField = null, string? conditionValue = null) =>
        new(
            id,
            $"Policy {id}",
            1,
            "platform",
            conditionField is not null
                ? new[] { new PolicyCondition(conditionField, "equals", conditionValue!) }
                : Array.Empty<PolicyCondition>(),
            new[] { new PolicyAction(action, new Dictionary<string, string>()) },
            DateTime.UtcNow
        );

    private static PolicyRegistryStore CreateRegistryWithPolicies(params PolicyDefinition[] policies)
    {
        var store = new PolicyRegistryStore();
        foreach (var policy in policies)
        {
            var record = new PolicyRecord(policy.PolicyId, policy.Version, policy, PolicyStatus.Active, DateTime.UtcNow);
            store.Register(record);
        }
        return store;
    }

    private static PolicySimulationEngine CreateEngine(params PolicyDefinition[] policies)
    {
        var registryStore = CreateRegistryWithPolicies(policies);
        return new PolicySimulationEngine(registryStore, new PolicyDependencyStore());
    }

    [Fact]
    public void SimulatePolicyEvaluation_SinglePolicy_ReturnsDecisions()
    {
        var policy = MakePolicy("p1");
        var engine = CreateEngine(policy);
        var request = new PolicySimulationRequest("platform", Guid.NewGuid().ToString(), new Dictionary<string, string>());

        var result = engine.SimulatePolicyEvaluation(request);

        Assert.Equal("platform", result.Domain);
        Assert.NotEmpty(result.Decisions);
    }

    [Fact]
    public void SimulatePolicyEvaluation_MultiplePolicies_EvaluatesAll()
    {
        var engine = CreateEngine(MakePolicy("p1", "allow"), MakePolicy("p2", "deny"));
        var request = new PolicySimulationRequest("platform", Guid.NewGuid().ToString(), new Dictionary<string, string>());

        var result = engine.SimulatePolicyEvaluation(request);

        Assert.Equal(2, result.Decisions.Count);
    }

    [Fact]
    public void SimulatePolicyEvaluation_ConditionMatching_EvaluatesCorrectly()
    {
        var engine = CreateEngine(MakePolicy("p1", "allow", "role", "admin"));
        var request = new PolicySimulationRequest(
            "platform",
            Guid.NewGuid().ToString(),
            new Dictionary<string, string> { ["role"] = "admin" });

        var result = engine.SimulatePolicyEvaluation(request);

        Assert.NotEmpty(result.Decisions);
        Assert.Contains(result.Decisions, d => d.Action == "allow");
    }

    [Fact]
    public void SimulatePolicyEvaluation_DeterministicResults_SameInputSameOutput()
    {
        var engine = CreateEngine(
            MakePolicy("p1", "allow", "role", "admin"),
            MakePolicy("p2", "deny", "role", "guest"));
        var actorId = Guid.NewGuid().ToString();
        var attributes = new Dictionary<string, string> { ["role"] = "admin" };

        var result1 = engine.SimulatePolicyEvaluation(new PolicySimulationRequest("platform", actorId, attributes));
        var result2 = engine.SimulatePolicyEvaluation(new PolicySimulationRequest("platform", actorId, attributes));

        Assert.Equal(result1.Decisions.Count, result2.Decisions.Count);
        for (var i = 0; i < result1.Decisions.Count; i++)
        {
            Assert.Equal(result1.Decisions[i].PolicyId, result2.Decisions[i].PolicyId);
            Assert.Equal(result1.Decisions[i].Allowed, result2.Decisions[i].Allowed);
            Assert.Equal(result1.Decisions[i].Action, result2.Decisions[i].Action);
        }
    }

    [Fact]
    public void SimulatePolicyEvaluation_IsolationTest_DoesNotMutatePolicies()
    {
        var policy = MakePolicy("p1", "allow");
        var engine = CreateEngine(policy);
        var request = new PolicySimulationRequest("platform", Guid.NewGuid().ToString(), new Dictionary<string, string>());

        var result = engine.SimulatePolicyEvaluation(request);

        // Verify original policy is unchanged
        Assert.Equal("p1", policy.PolicyId);
        Assert.Equal("Policy p1", policy.Name);
        Assert.Equal(1, policy.Version);
        Assert.Equal("platform", policy.TargetDomain);

        // Verify simulation produced results
        Assert.NotEmpty(result.Decisions);
    }

    [Fact]
    public void SimulatePolicyEvaluation_IncludesActorId()
    {
        var engine = CreateEngine(MakePolicy("p1"));
        var actorId = Guid.NewGuid().ToString();
        var request = new PolicySimulationRequest("platform", actorId, new Dictionary<string, string>());

        var result = engine.SimulatePolicyEvaluation(request);

        Assert.Equal(actorId, result.ActorId);
    }

    [Fact]
    public void SimulatePolicyEvaluation_ConcurrentSafety_ProducesSameResults()
    {
        var engine = CreateEngine(MakePolicy("p1", "allow"), MakePolicy("p2", "deny"));
        var actorId = Guid.NewGuid().ToString();
        var attributes = new Dictionary<string, string>();

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => engine.SimulatePolicyEvaluation(
                new PolicySimulationRequest("platform", actorId, attributes))))
            .ToArray();

        Task.WaitAll(tasks);

        var expected = tasks[0].Result;
        foreach (var task in tasks)
        {
            var actual = task.Result;
            Assert.Equal(expected.Decisions.Count, actual.Decisions.Count);
            for (var i = 0; i < expected.Decisions.Count; i++)
            {
                Assert.Equal(expected.Decisions[i].PolicyId, actual.Decisions[i].PolicyId);
                Assert.Equal(expected.Decisions[i].Allowed, actual.Decisions[i].Allowed);
                Assert.Equal(expected.Decisions[i].Action, actual.Decisions[i].Action);
            }
        }
    }
}
