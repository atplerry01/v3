using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyRegistryTests
{
    private readonly PolicyRegistryStore _store = new();
    private readonly PolicyRegistryEngine _engine;

    public PolicyRegistryTests()
    {
        _engine = new PolicyRegistryEngine(_store);
    }

    private static PolicyDefinition CreateDefinition(string id = "test-policy", int version = 1) =>
        new(
            id,
            $"Test Policy {id}",
            version,
            "identity",
            new List<PolicyCondition> { new("status", "equals", "active") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) },
            DateTime.UtcNow
        );

    [Fact]
    public void RegisterPolicy_ValidDefinition_ReturnsPolicyRecord()
    {
        var definition = CreateDefinition();

        var record = _engine.RegisterPolicy(definition);

        Assert.Equal("test-policy", record.PolicyId);
        Assert.Equal(1, record.Version);
        Assert.Equal(PolicyStatus.Active, record.Status);
        Assert.Equal(definition, record.PolicyDefinition);
    }

    [Fact]
    public void RegisterPolicy_DuplicateIdAndVersion_ThrowsInvalidOperationException()
    {
        var definition = CreateDefinition();
        _engine.RegisterPolicy(definition);

        var ex = Assert.Throws<InvalidOperationException>(() => _engine.RegisterPolicy(definition));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void GetPolicy_ExistingPolicy_ReturnsPolicyRecord()
    {
        var definition = CreateDefinition();
        _engine.RegisterPolicy(definition);

        var result = _engine.GetPolicy("test-policy");

        Assert.Equal("test-policy", result.PolicyId);
    }

    [Fact]
    public void GetPolicy_NonExistentPolicy_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => _engine.GetPolicy("non-existent"));
    }

    [Fact]
    public void GetPolicies_MultiplePolicies_ReturnsAll()
    {
        _engine.RegisterPolicy(CreateDefinition("policy-a"));
        _engine.RegisterPolicy(CreateDefinition("policy-b"));

        var policies = _engine.GetPolicies();

        Assert.Equal(2, policies.Count);
    }

    [Fact]
    public void GetPolicies_Empty_ReturnsEmptyList()
    {
        var policies = _engine.GetPolicies();

        Assert.Empty(policies);
    }

    [Fact]
    public void RegisterPolicy_NewerVersion_Succeeds()
    {
        _engine.RegisterPolicy(CreateDefinition("versioned-policy", version: 1));

        var record = _engine.RegisterPolicy(CreateDefinition("versioned-policy", version: 2));

        Assert.Equal(2, record.Version);
    }
}
