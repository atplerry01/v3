using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyDependencyEngineTests
{
    private static PolicyDefinition MakePolicy(string id) =>
        new(id, $"Policy {id}", 1, "platform", Array.Empty<PolicyCondition>(), Array.Empty<PolicyAction>(), DateTime.UtcNow);

    [Fact]
    public void ResolvePolicyExecutionOrder_SimpleChain_ReturnsDependenciesFirst()
    {
        // A depends on B, B depends on C => order: C, B, A
        var engine = new PolicyDependencyEngine();
        var input = new PolicyDependencyInput(
            new[] { MakePolicy("a"), MakePolicy("b"), MakePolicy("c") },
            new Dictionary<string, List<string>>
            {
                ["a"] = new() { "b" },
                ["b"] = new() { "c" }
            });

        var result = engine.ResolvePolicyExecutionOrder(input);

        Assert.Equal(3, result.OrderedPolicies.Count);
        var ids = result.OrderedPolicies.Select(p => p.PolicyId).ToList();
        Assert.Equal("c", ids[0]);
        Assert.Equal("b", ids[1]);
        Assert.Equal("a", ids[2]);
        Assert.Empty(result.DetectedCycles);
        Assert.Empty(result.MissingDependencies);
    }

    [Fact]
    public void ResolvePolicyExecutionOrder_MultipleDependencyGraph_ResolvesCorrectly()
    {
        // A -> B, A -> C, B -> D, C -> D (diamond)
        var engine = new PolicyDependencyEngine();
        var input = new PolicyDependencyInput(
            new[] { MakePolicy("a"), MakePolicy("b"), MakePolicy("c"), MakePolicy("d") },
            new Dictionary<string, List<string>>
            {
                ["a"] = new() { "b", "c" },
                ["b"] = new() { "d" },
                ["c"] = new() { "d" }
            });

        var result = engine.ResolvePolicyExecutionOrder(input);

        Assert.Equal(4, result.OrderedPolicies.Count);
        var ids = result.OrderedPolicies.Select(p => p.PolicyId).ToList();
        Assert.True(ids.IndexOf("d") < ids.IndexOf("b"));
        Assert.True(ids.IndexOf("d") < ids.IndexOf("c"));
        Assert.True(ids.IndexOf("b") < ids.IndexOf("a"));
        Assert.True(ids.IndexOf("c") < ids.IndexOf("a"));
        Assert.Empty(result.DetectedCycles);
    }

    [Fact]
    public void ResolvePolicyExecutionOrder_MissingDependency_ReportsMissing()
    {
        var engine = new PolicyDependencyEngine();
        var input = new PolicyDependencyInput(
            new[] { MakePolicy("a"), MakePolicy("b") },
            new Dictionary<string, List<string>>
            {
                ["a"] = new() { "b", "nonexistent" }
            });

        var result = engine.ResolvePolicyExecutionOrder(input);

        Assert.Contains("nonexistent", result.MissingDependencies);
    }

    [Fact]
    public void ResolvePolicyExecutionOrder_CircularDependency_DetectsCycle()
    {
        var engine = new PolicyDependencyEngine();
        var input = new PolicyDependencyInput(
            new[] { MakePolicy("a"), MakePolicy("b"), MakePolicy("c") },
            new Dictionary<string, List<string>>
            {
                ["a"] = new() { "b" },
                ["b"] = new() { "c" },
                ["c"] = new() { "a" }
            });

        var result = engine.ResolvePolicyExecutionOrder(input);

        Assert.NotEmpty(result.DetectedCycles);
        Assert.Empty(result.OrderedPolicies);
    }

    [Fact]
    public void ResolvePolicyExecutionOrder_DeterministicOrdering_SameInputSameOutput()
    {
        var engine = new PolicyDependencyEngine();
        var policies = new[] { MakePolicy("z"), MakePolicy("m"), MakePolicy("a") };
        var deps = new Dictionary<string, List<string>>
        {
            ["z"] = new() { "a" },
            ["m"] = new() { "a" }
        };
        var input = new PolicyDependencyInput(policies, deps);

        var result1 = engine.ResolvePolicyExecutionOrder(input);
        var result2 = engine.ResolvePolicyExecutionOrder(input);

        var ids1 = result1.OrderedPolicies.Select(p => p.PolicyId).ToList();
        var ids2 = result2.OrderedPolicies.Select(p => p.PolicyId).ToList();
        Assert.Equal(ids1, ids2);
    }

    [Fact]
    public void ResolvePolicyExecutionOrder_LargeDependencyGraph_ResolvesCorrectly()
    {
        var engine = new PolicyDependencyEngine();
        var policies = new List<PolicyDefinition>();
        var deps = new Dictionary<string, List<string>>();

        // Create a chain of 100 policies: p-000 -> p-001 -> ... -> p-099
        for (var i = 0; i < 100; i++)
        {
            var id = $"p-{i:D3}";
            policies.Add(MakePolicy(id));
            if (i > 0)
            {
                deps[id] = new List<string> { $"p-{i - 1:D3}" };
            }
        }

        var input = new PolicyDependencyInput(policies, deps);
        var result = engine.ResolvePolicyExecutionOrder(input);

        Assert.Equal(100, result.OrderedPolicies.Count);
        Assert.Empty(result.DetectedCycles);
        // p-000 (no deps) should be first, p-099 (depends on all others) should be last
        Assert.Equal("p-000", result.OrderedPolicies[0].PolicyId);
        Assert.Equal("p-099", result.OrderedPolicies[99].PolicyId);
    }

    [Fact]
    public void ResolvePolicyExecutionOrder_ConcurrentExecutionSafety_ProducesSameResults()
    {
        var engine = new PolicyDependencyEngine();
        var policies = new[] { MakePolicy("x"), MakePolicy("y"), MakePolicy("z") };
        var deps = new Dictionary<string, List<string>>
        {
            ["x"] = new() { "y" },
            ["y"] = new() { "z" }
        };
        var input = new PolicyDependencyInput(policies, deps);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => engine.ResolvePolicyExecutionOrder(input)))
            .ToArray();

        Task.WaitAll(tasks);

        var expected = tasks[0].Result.OrderedPolicies.Select(p => p.PolicyId).ToList();
        foreach (var task in tasks)
        {
            var actual = task.Result.OrderedPolicies.Select(p => p.PolicyId).ToList();
            Assert.Equal(expected, actual);
            Assert.Empty(task.Result.DetectedCycles);
        }
    }

    [Fact]
    public void ResolvePolicyExecutionOrder_NoDependencies_ReturnsAllInDeterministicOrder()
    {
        var engine = new PolicyDependencyEngine();
        var input = new PolicyDependencyInput(
            new[] { MakePolicy("c"), MakePolicy("a"), MakePolicy("b") },
            new Dictionary<string, List<string>>());

        var result = engine.ResolvePolicyExecutionOrder(input);

        Assert.Equal(3, result.OrderedPolicies.Count);
        Assert.Empty(result.DetectedCycles);
        Assert.Empty(result.MissingDependencies);
        // Deterministic: sorted by policy ID
        Assert.Equal("a", result.OrderedPolicies[0].PolicyId);
        Assert.Equal("b", result.OrderedPolicies[1].PolicyId);
        Assert.Equal("c", result.OrderedPolicies[2].PolicyId);
    }

    [Fact]
    public void ResolvePolicyExecutionOrder_EmptyInput_ReturnsEmptyResult()
    {
        var engine = new PolicyDependencyEngine();
        var input = new PolicyDependencyInput(
            Array.Empty<PolicyDefinition>(),
            new Dictionary<string, List<string>>());

        var result = engine.ResolvePolicyExecutionOrder(input);

        Assert.Empty(result.OrderedPolicies);
        Assert.Empty(result.DetectedCycles);
        Assert.Empty(result.MissingDependencies);
    }
}
