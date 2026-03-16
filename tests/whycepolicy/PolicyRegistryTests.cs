using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Registry;

namespace Whycespace.WhycePolicy.Tests;

public sealed class PolicyRegistryTests
{
    private readonly PolicyRegistry _registry = new();

    private static PolicyDefinition CreateDefinition(
        string id = "test-policy",
        string name = "Test Policy",
        string domain = "identity",
        int version = 1) =>
        new(
            id,
            name,
            version,
            domain,
            new List<PolicyCondition> { new("status", "equals", "active") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) },
            DateTime.UtcNow
        );

    [Fact]
    public void RegisterPolicy_ValidDefinition_CreatesEntry()
    {
        var definition = CreateDefinition();

        _registry.RegisterPolicy(definition);

        var entry = _registry.GetPolicyById("test-policy");
        Assert.NotNull(entry);
        Assert.Equal("test-policy", entry.PolicyId);
        Assert.Equal("Test Policy", entry.PolicyName);
        Assert.Equal("identity", entry.Domain);
        Assert.Equal(1, entry.CurrentVersion);
        Assert.Equal(PolicyLifecycleState.Active, entry.LifecycleState);
    }

    [Fact]
    public void RegisterPolicy_DuplicateId_IncrementsVersion()
    {
        _registry.RegisterPolicy(CreateDefinition());
        _registry.RegisterPolicy(CreateDefinition());

        var entry = _registry.GetPolicyById("test-policy");
        Assert.NotNull(entry);
        Assert.Equal(2, entry.CurrentVersion);
    }

    [Fact]
    public void GetPolicyById_ExistingPolicy_ReturnsEntry()
    {
        _registry.RegisterPolicy(CreateDefinition("my-policy", "My Policy"));

        var entry = _registry.GetPolicyById("my-policy");

        Assert.NotNull(entry);
        Assert.Equal("my-policy", entry.PolicyId);
        Assert.Equal("My Policy", entry.PolicyName);
    }

    [Fact]
    public void GetPolicyById_NonExistent_ReturnsNull()
    {
        var entry = _registry.GetPolicyById("does-not-exist");

        Assert.Null(entry);
    }

    [Fact]
    public void GetPoliciesByDomain_MatchingDomain_ReturnsPolicies()
    {
        _registry.RegisterPolicy(CreateDefinition("p1", domain: "identity"));
        _registry.RegisterPolicy(CreateDefinition("p2", domain: "economic"));
        _registry.RegisterPolicy(CreateDefinition("p3", domain: "identity"));

        var identityPolicies = _registry.GetPoliciesByDomain("identity");

        Assert.Equal(2, identityPolicies.Count);
        Assert.All(identityPolicies, p => Assert.Equal("identity", p.Domain));
    }

    [Fact]
    public void GetPoliciesByDomain_NoMatch_ReturnsEmpty()
    {
        _registry.RegisterPolicy(CreateDefinition("p1", domain: "identity"));

        var result = _registry.GetPoliciesByDomain("governance");

        Assert.Empty(result);
    }

    [Fact]
    public void GetActivePolicies_ReturnsOnlyActiveEntries()
    {
        _registry.RegisterPolicy(CreateDefinition("p1"));
        _registry.RegisterPolicy(CreateDefinition("p2"));

        var active = _registry.GetActivePolicies();

        Assert.Equal(2, active.Count);
        Assert.All(active, p => Assert.Equal(PolicyLifecycleState.Active, p.LifecycleState));
    }

    [Fact]
    public void GetPolicyVersions_TracksAllVersions()
    {
        _registry.RegisterPolicy(CreateDefinition("versioned"));
        _registry.RegisterPolicy(CreateDefinition("versioned"));
        _registry.RegisterPolicy(CreateDefinition("versioned"));

        var versions = _registry.GetPolicyVersions("versioned");

        Assert.Equal(3, versions.Count);
        Assert.Equal(1, versions[0].Version);
        Assert.Equal(2, versions[1].Version);
        Assert.Equal(3, versions[2].Version);
    }

    [Fact]
    public void GetPolicyVersions_NonExistent_ReturnsEmpty()
    {
        var versions = _registry.GetPolicyVersions("no-such-policy");

        Assert.Empty(versions);
    }

    [Fact]
    public void ConcurrentRegistration_IsThreadSafe()
    {
        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() =>
                _registry.RegisterPolicy(CreateDefinition($"concurrent-{i}"))))
            .ToArray();

        Task.WaitAll(tasks);

        var active = _registry.GetActivePolicies();
        Assert.Equal(100, active.Count);
    }
}
