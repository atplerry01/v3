using Whycespace.Engines.T0U.WhycePolicy.Governance.Dependency;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyDependencyEngineTests
{
    private static PolicyDependencyEngine CreateEngine(PolicyDependencyStore? store = null)
    {
        return new PolicyDependencyEngine(store ?? new PolicyDependencyStore());
    }

    [Fact]
    public void RegisterDependency_SimpleChain_RegistersSuccessfully()
    {
        var store = new PolicyDependencyStore();
        var engine = CreateEngine(store);

        var dep = engine.RegisterDependency("a", "b");

        Assert.Equal("a", dep.PolicyId);
        Assert.Equal("b", dep.DependsOnPolicyId);
    }

    [Fact]
    public void ResolveDependencyGraph_SimpleChain_ReturnsDependenciesFirst()
    {
        // A depends on B, B depends on C => resolve("a") should return: C, B, A
        var store = new PolicyDependencyStore();
        var engine = CreateEngine(store);
        engine.RegisterDependency("a", "b");
        engine.RegisterDependency("b", "c");

        var result = engine.ResolveDependencyGraph("a");

        Assert.Equal(3, result.Count);
        Assert.Equal("c", result[0]);
        Assert.Equal("b", result[1]);
        Assert.Equal("a", result[2]);
    }

    [Fact]
    public void ResolveDependencyGraph_DiamondDependency_ResolvesCorrectly()
    {
        // A -> B, A -> C, B -> D, C -> D (diamond)
        var store = new PolicyDependencyStore();
        var engine = CreateEngine(store);
        engine.RegisterDependency("a", "b");
        engine.RegisterDependency("a", "c");
        engine.RegisterDependency("b", "d");
        engine.RegisterDependency("c", "d");

        var result = engine.ResolveDependencyGraph("a").ToList();

        Assert.Equal(4, result.Count);
        Assert.True(result.IndexOf("d") < result.IndexOf("b"));
        Assert.True(result.IndexOf("d") < result.IndexOf("c"));
        Assert.True(result.IndexOf("b") < result.IndexOf("a"));
        Assert.True(result.IndexOf("c") < result.IndexOf("a"));
    }

    [Fact]
    public void GetDependencies_ReturnsDirectDependencies()
    {
        var store = new PolicyDependencyStore();
        var engine = CreateEngine(store);
        engine.RegisterDependency("a", "b");
        engine.RegisterDependency("a", "c");

        var deps = engine.GetDependencies("a");

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.DependsOnPolicyId == "b");
        Assert.Contains(deps, d => d.DependsOnPolicyId == "c");
    }

    [Fact]
    public void HasCircularDependency_DetectsCycle()
    {
        var store = new PolicyDependencyStore();
        var engine = CreateEngine(store);
        engine.RegisterDependency("a", "b");
        engine.RegisterDependency("b", "c");

        // Adding c -> a would create a cycle
        Assert.True(engine.HasCircularDependency("c", "a"));
    }

    [Fact]
    public void RegisterDependency_CircularDependency_ThrowsInvalidOperation()
    {
        var store = new PolicyDependencyStore();
        var engine = CreateEngine(store);
        engine.RegisterDependency("a", "b");
        engine.RegisterDependency("b", "c");

        Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterDependency("c", "a"));
    }

    [Fact]
    public void RegisterDependency_SelfDependency_ThrowsInvalidOperation()
    {
        var engine = CreateEngine();

        Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterDependency("a", "a"));
    }

    [Fact]
    public void RegisterDependency_DuplicateDependency_ThrowsInvalidOperation()
    {
        var store = new PolicyDependencyStore();
        var engine = CreateEngine(store);
        engine.RegisterDependency("a", "b");

        Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterDependency("a", "b"));
    }

    [Fact]
    public void ResolveDependencyGraph_NoDependencies_ReturnsSinglePolicy()
    {
        var engine = CreateEngine();

        var result = engine.ResolveDependencyGraph("standalone");

        Assert.Single(result);
        Assert.Equal("standalone", result[0]);
    }

    [Fact]
    public void ResolveDependencyGraph_LargeChain_ResolvesCorrectly()
    {
        var store = new PolicyDependencyStore();
        var engine = CreateEngine(store);

        // Create a chain of 100 policies: p-001 -> p-000, p-002 -> p-001, ..., p-099 -> p-098
        for (var i = 1; i < 100; i++)
        {
            engine.RegisterDependency($"p-{i:D3}", $"p-{i - 1:D3}");
        }

        var result = engine.ResolveDependencyGraph("p-099");

        Assert.Equal(100, result.Count);
        // p-000 (no deps) should be first, p-099 should be last
        Assert.Equal("p-000", result[0]);
        Assert.Equal("p-099", result[99]);
    }

    [Fact]
    public void ResolveDependencyGraph_ConcurrentSafety_ProducesSameResults()
    {
        var store = new PolicyDependencyStore();
        var engine = CreateEngine(store);
        engine.RegisterDependency("x", "y");
        engine.RegisterDependency("y", "z");

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => engine.ResolveDependencyGraph("x")))
            .ToArray();

        Task.WaitAll(tasks);

        var expected = tasks[0].Result;
        foreach (var task in tasks)
        {
            Assert.Equal(expected, task.Result);
        }
    }

    [Fact]
    public void RegisterDependency_EmptyPolicyId_ThrowsArgumentException()
    {
        var engine = CreateEngine();

        Assert.Throws<ArgumentException>(() =>
            engine.RegisterDependency("", "b"));
    }

    [Fact]
    public void RegisterDependency_EmptyDependsOnId_ThrowsArgumentException()
    {
        var engine = CreateEngine();

        Assert.Throws<ArgumentException>(() =>
            engine.RegisterDependency("a", ""));
    }
}
