using Whycespace.Engines.T0U.WhycePolicy.Enforcement.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Cache;
using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Engines;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyEnforcementTests
{
    private readonly PolicyRegistryStore _registryStore = new();
    private readonly PolicyDependencyStore _dependencyStore = new();
    private readonly PolicyContextStore _contextStore = new();
    private readonly PolicyDecisionCacheStore _cacheStore = new();
    private readonly PolicyEnforcementEngine _engine;

    public PolicyEnforcementTests()
    {
        var evaluationEngine = new PolicyEvaluationEngine(_registryStore, _dependencyStore);
        var contextEngine = new PolicyContextEngine(_contextStore);
        var cacheEngine = new PolicyDecisionCacheEngine(_cacheStore);
        _engine = new PolicyEnforcementEngine(evaluationEngine, contextEngine, cacheEngine);
    }

    private static PolicyDefinition CreatePolicy(string id, string domain, List<PolicyCondition> conditions, List<PolicyAction> actions)
    {
        return new PolicyDefinition(id, $"Policy {id}", 1, domain, conditions, actions, DateTime.UtcNow);
    }

    private void RegisterPolicy(PolicyDefinition policy)
    {
        var record = new PolicyRecord(policy.PolicyId, policy.Version, policy, PolicyStatus.Active, DateTime.UtcNow);
        _registryStore.Register(record);
    }

    private static PolicyEnforcementRequest CreateRequest(string domain, Dictionary<string, string> attributes)
    {
        return new PolicyEnforcementRequest(Guid.NewGuid().ToString(), domain, "test-operation", attributes);
    }

    [Fact]
    public void EnforcePolicy_AllowRequest_ReturnsAllowed()
    {
        var policy = CreatePolicy("enforce-allow", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy(policy);

        var request = CreateRequest("identity", new Dictionary<string, string> { ["trust_score"] = "75" });
        var result = _engine.EnforcePolicy(request);

        Assert.True(result.Allowed);
        Assert.Equal("All policies passed", result.Reason);
    }

    [Fact]
    public void EnforcePolicy_DenyRequest_ReturnsDenied()
    {
        var policy = CreatePolicy("enforce-deny", "access",
            new List<PolicyCondition> { new("trust_score", "less_than", "50") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string> { ["reason"] = "Trust too low" }) });

        RegisterPolicy(policy);

        var request = CreateRequest("access", new Dictionary<string, string> { ["trust_score"] = "30" });
        var result = _engine.EnforcePolicy(request);

        Assert.False(result.Allowed);
        Assert.Contains("deny", result.Reason);
    }

    [Fact]
    public void EnforcePolicy_MultiplePolicies_AllEvaluated()
    {
        var allowPolicy = CreatePolicy("multi-allow", "payments",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var logPolicy = CreatePolicy("multi-log", "payments",
            new List<PolicyCondition> { new("trust_score", "greater_than", "10") },
            new List<PolicyAction> { new("log", new Dictionary<string, string>()) });

        RegisterPolicy(allowPolicy);
        RegisterPolicy(logPolicy);

        var request = CreateRequest("payments", new Dictionary<string, string> { ["trust_score"] = "75" });
        var result = _engine.EnforcePolicy(request);

        Assert.True(result.Allowed);
        Assert.Equal(2, result.Decisions.Count);
    }

    [Fact]
    public void EnforcePolicy_CacheHit_ReturnsCachedResult()
    {
        var policy = CreatePolicy("cache-hit", "cache-domain",
            new List<PolicyCondition> { new("level", "equals", "high") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy(policy);

        var request = CreateRequest("cache-domain", new Dictionary<string, string> { ["level"] = "high" });

        var firstResult = _engine.EnforcePolicy(request);
        var secondResult = _engine.EnforcePolicy(request);

        Assert.True(firstResult.Allowed);
        Assert.True(secondResult.Allowed);
        Assert.Equal(firstResult.Decisions.Count, secondResult.Decisions.Count);
    }

    [Fact]
    public void EnforcePolicy_CacheMiss_EvaluatesFresh()
    {
        var policy = CreatePolicy("cache-miss", "fresh-domain",
            new List<PolicyCondition> { new("status", "equals", "active") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy(policy);

        var request1 = CreateRequest("fresh-domain", new Dictionary<string, string> { ["status"] = "active" });
        var request2 = CreateRequest("fresh-domain", new Dictionary<string, string> { ["status"] = "inactive" });

        var result1 = _engine.EnforcePolicy(request1);
        var result2 = _engine.EnforcePolicy(request2);

        Assert.True(result1.Allowed);
        Assert.True(result2.Allowed);
    }

    [Fact]
    public void EnforcePolicy_DenyBlocks_MiddlewareWouldReturn403()
    {
        var policy = CreatePolicy("block-deny", "restricted",
            new List<PolicyCondition> { new("clearance", "less_than", "5") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string> { ["reason"] = "Insufficient clearance" }) });

        RegisterPolicy(policy);

        var request = CreateRequest("restricted", new Dictionary<string, string> { ["clearance"] = "2" });
        var result = _engine.EnforcePolicy(request);

        Assert.False(result.Allowed);
        Assert.NotNull(result.Reason);
        Assert.NotEmpty(result.Decisions);
    }

    [Fact]
    public void EnforcePolicy_AllowPasses_MiddlewareWouldContinue()
    {
        var policy = CreatePolicy("pass-allow", "open",
            new List<PolicyCondition> { new("clearance", "greater_than", "1") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy(policy);

        var request = CreateRequest("open", new Dictionary<string, string> { ["clearance"] = "10" });
        var result = _engine.EnforcePolicy(request);

        Assert.True(result.Allowed);
        Assert.Equal("All policies passed", result.Reason);
    }
}
