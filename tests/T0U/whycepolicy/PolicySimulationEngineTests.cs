using Whycespace.Engines.T0U.WhycePolicy;
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

    private static PolicyContext MakeContext(string domain = "platform", Dictionary<string, string>? attributes = null) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            domain,
            attributes ?? new Dictionary<string, string>(),
            DateTime.UtcNow
        );

    private static PolicySimulationEngine CreateEngine()
    {
        return new PolicySimulationEngine(new PolicyRegistryStore(), new PolicyDependencyStore());
    }

    [Fact]
    public void RunSimulation_SinglePolicyEvaluation_ReturnsDecision()
    {
        var engine = CreateEngine();
        var input = new PolicySimulationInput(
            new[] { MakePolicy("p1") },
            new[] { MakeContext() }
        );

        var result = engine.RunSimulation(input);

        Assert.Equal(1, result.SimulationCount);
        Assert.Single(result.SimulationRecords);
        Assert.Single(result.SimulationRecords[0].Decisions);
        Assert.True(result.SimulationRecords[0].FinalDecision.Allowed);
    }

    [Fact]
    public void RunSimulation_MultiplePolicies_EvaluatesAll()
    {
        var engine = CreateEngine();
        var input = new PolicySimulationInput(
            new[] { MakePolicy("p1", "allow"), MakePolicy("p2", "deny") },
            new[] { MakeContext() }
        );

        var result = engine.RunSimulation(input);

        Assert.Equal(1, result.SimulationCount);
        Assert.Equal(2, result.SimulationRecords[0].Decisions.Count);
        Assert.False(result.SimulationRecords[0].FinalDecision.Allowed);
    }

    [Fact]
    public void RunSimulation_MultipleContexts_EvaluatesEachContext()
    {
        var engine = CreateEngine();
        var input = new PolicySimulationInput(
            new[] { MakePolicy("p1") },
            new[] { MakeContext(), MakeContext(), MakeContext() }
        );

        var result = engine.RunSimulation(input);

        Assert.Equal(3, result.SimulationCount);
        Assert.Equal(3, result.SimulationRecords.Count);
    }

    [Fact]
    public void RunSimulation_DeterministicResults_SameInputSameOutput()
    {
        var engine = CreateEngine();
        var context = MakeContext(attributes: new Dictionary<string, string> { ["role"] = "admin" });
        var policies = new[] { MakePolicy("p1", "allow", "role", "admin"), MakePolicy("p2", "deny", "role", "guest") };
        var input = new PolicySimulationInput(policies, new[] { context });

        var result1 = engine.RunSimulation(input);
        var result2 = engine.RunSimulation(input);

        Assert.Equal(result1.SimulationCount, result2.SimulationCount);
        for (var i = 0; i < result1.SimulationRecords.Count; i++)
        {
            var r1 = result1.SimulationRecords[i];
            var r2 = result2.SimulationRecords[i];
            Assert.Equal(r1.Decisions.Count, r2.Decisions.Count);
            for (var j = 0; j < r1.Decisions.Count; j++)
            {
                Assert.Equal(r1.Decisions[j].PolicyId, r2.Decisions[j].PolicyId);
                Assert.Equal(r1.Decisions[j].Allowed, r2.Decisions[j].Allowed);
                Assert.Equal(r1.Decisions[j].Action, r2.Decisions[j].Action);
            }
            Assert.Equal(r1.FinalDecision.Allowed, r2.FinalDecision.Allowed);
            Assert.Equal(r1.FinalDecision.Action, r2.FinalDecision.Action);
        }
    }

    [Fact]
    public void RunSimulation_IsolationTest_DoesNotMutatePolicies()
    {
        var engine = CreateEngine();
        var policy = MakePolicy("p1", "allow");
        var context = MakeContext();
        var input = new PolicySimulationInput(new[] { policy }, new[] { context });

        var result = engine.RunSimulation(input);

        // Verify original policy is unchanged
        Assert.Equal("p1", policy.PolicyId);
        Assert.Equal("Policy p1", policy.Name);
        Assert.Equal(1, policy.Version);
        Assert.Equal("platform", policy.TargetDomain);

        // Verify simulation produced results
        Assert.Equal(1, result.SimulationCount);
    }

    [Fact]
    public void RunSimulation_LargeContextSet_HandlesCorrectly()
    {
        var engine = CreateEngine();
        var contexts = Enumerable.Range(0, 100)
            .Select(_ => MakeContext())
            .ToList();

        var input = new PolicySimulationInput(
            new[] { MakePolicy("p1"), MakePolicy("p2") },
            contexts
        );

        var result = engine.RunSimulation(input);

        Assert.Equal(100, result.SimulationCount);
        Assert.Equal(100, result.SimulationRecords.Count);
        Assert.All(result.SimulationRecords, r => Assert.Equal(2, r.Decisions.Count));
    }

    [Fact]
    public void RunSimulation_ConcurrentSafety_ProducesSameResults()
    {
        var engine = CreateEngine();
        var input = new PolicySimulationInput(
            new[] { MakePolicy("p1", "allow"), MakePolicy("p2", "deny") },
            new[] { MakeContext(), MakeContext() }
        );

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => engine.RunSimulation(input)))
            .ToArray();

        Task.WaitAll(tasks);

        var expected = tasks[0].Result;
        foreach (var task in tasks)
        {
            var actual = task.Result;
            Assert.Equal(expected.SimulationCount, actual.SimulationCount);
            for (var i = 0; i < expected.SimulationRecords.Count; i++)
            {
                Assert.Equal(
                    expected.SimulationRecords[i].FinalDecision.Allowed,
                    actual.SimulationRecords[i].FinalDecision.Allowed);
                Assert.Equal(
                    expected.SimulationRecords[i].FinalDecision.Action,
                    actual.SimulationRecords[i].FinalDecision.Action);
            }
        }
    }
}
