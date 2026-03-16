using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyDependencyTests
{
    private readonly PolicyDependencyStore _store = new();
    private readonly PolicyDependencyEngine _engine;

    public PolicyDependencyTests()
    {
        _engine = new PolicyDependencyEngine(_store);
    }

    [Fact]
    public void RegisterDependency_ValidInput_ReturnsDependency()
    {
        var result = _engine.RegisterDependency("policy-a", "policy-b");

        Assert.Equal("policy-a", result.PolicyId);
        Assert.Equal("policy-b", result.DependsOnPolicyId);
    }

    [Fact]
    public void ResolveDependencyGraph_ChainedDependencies_ReturnsDependenciesFirst()
    {
        _engine.RegisterDependency("policy-a", "policy-b");
        _engine.RegisterDependency("policy-b", "policy-c");

        var resolved = _engine.ResolveDependencyGraph("policy-a");

        Assert.Equal(3, resolved.Count);
        Assert.Equal("policy-c", resolved[0]);
        Assert.Equal("policy-b", resolved[1]);
        Assert.Equal("policy-a", resolved[2]);
    }

    [Fact]
    public void RegisterDependency_CircularDependency_ThrowsInvalidOperationException()
    {
        _engine.RegisterDependency("policy-a", "policy-b");
        _engine.RegisterDependency("policy-b", "policy-c");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterDependency("policy-c", "policy-a"));
        Assert.Contains("Circular dependency", ex.Message);
    }

    [Fact]
    public void RegisterDependency_SelfDependency_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterDependency("policy-a", "policy-a"));
        Assert.Contains("cannot depend on itself", ex.Message);
    }

    [Fact]
    public void RegisterDependency_DuplicateDependency_ThrowsInvalidOperationException()
    {
        _engine.RegisterDependency("policy-a", "policy-b");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterDependency("policy-a", "policy-b"));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void GetDependencies_NoDependencies_ReturnsEmptyList()
    {
        var deps = _engine.GetDependencies("non-existent");

        Assert.Empty(deps);
    }

    [Fact]
    public void ResolveDependencyGraph_NoDependencies_ReturnsSinglePolicy()
    {
        var resolved = _engine.ResolveDependencyGraph("standalone-policy");

        Assert.Single(resolved);
        Assert.Equal("standalone-policy", resolved[0]);
    }

    [Fact]
    public void HasCircularDependency_DirectCycle_ReturnsTrue()
    {
        _engine.RegisterDependency("policy-a", "policy-b");

        Assert.True(_engine.HasCircularDependency("policy-b", "policy-a"));
    }

    [Fact]
    public void HasCircularDependency_NoCycle_ReturnsFalse()
    {
        _engine.RegisterDependency("policy-a", "policy-b");

        Assert.False(_engine.HasCircularDependency("policy-a", "policy-c"));
    }

    [Fact]
    public void ResolveDependencyGraph_DiamondDependency_ResolvesCorrectly()
    {
        // A -> B, A -> C, B -> D, C -> D
        _engine.RegisterDependency("policy-a", "policy-b");
        _engine.RegisterDependency("policy-a", "policy-c");
        _engine.RegisterDependency("policy-b", "policy-d");
        _engine.RegisterDependency("policy-c", "policy-d");

        var resolved = _engine.ResolveDependencyGraph("policy-a").ToList();

        Assert.Equal(4, resolved.Count);
        // D must come before B and C, which must come before A
        Assert.True(resolved.IndexOf("policy-d") < resolved.IndexOf("policy-b"));
        Assert.True(resolved.IndexOf("policy-d") < resolved.IndexOf("policy-c"));
        Assert.True(resolved.IndexOf("policy-b") < resolved.IndexOf("policy-a"));
        Assert.True(resolved.IndexOf("policy-c") < resolved.IndexOf("policy-a"));
    }
}
