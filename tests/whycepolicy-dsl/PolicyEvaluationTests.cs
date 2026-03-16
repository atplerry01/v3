using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyEvaluationTests
{
    private readonly PolicyRegistryStore _registryStore = new();
    private readonly PolicyDependencyStore _dependencyStore = new();
    private readonly PolicyEvaluationEngine _engine;

    public PolicyEvaluationTests()
    {
        _engine = new PolicyEvaluationEngine(_registryStore, _dependencyStore);
    }

    private static PolicyContext CreateContext(string domain, Dictionary<string, string> attributes)
    {
        return new PolicyContext(Guid.NewGuid(), Guid.NewGuid(), domain, attributes, DateTime.UtcNow);
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

    [Fact]
    public void EvaluatePolicy_AllowAction_ReturnsAllowedTrue()
    {
        var policy = CreatePolicy("pol-1", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var context = CreateContext("identity", new Dictionary<string, string> { ["trust_score"] = "75" });

        var decision = _engine.EvaluatePolicy(policy, context);

        Assert.True(decision.Allowed);
        Assert.Equal("allow", decision.Action);
        Assert.Equal("pol-1", decision.PolicyId);
    }

    [Fact]
    public void EvaluatePolicy_DenyAction_ReturnsAllowedFalse()
    {
        var policy = CreatePolicy("pol-2", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "50") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var context = CreateContext("identity", new Dictionary<string, string> { ["trust_score"] = "30" });

        var decision = _engine.EvaluatePolicy(policy, context);

        Assert.False(decision.Allowed);
        Assert.Equal("deny", decision.Action);
    }

    [Fact]
    public void EvaluatePolicy_MultipleConditions_AllMustMatch()
    {
        var policy = CreatePolicy("pol-3", "identity",
            new List<PolicyCondition>
            {
                new("trust_score", "greater_than", "50"),
                new("region", "equals", "us-east")
            },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var context = CreateContext("identity", new Dictionary<string, string>
        {
            ["trust_score"] = "75",
            ["region"] = "us-east"
        });

        var decision = _engine.EvaluatePolicy(policy, context);

        Assert.True(decision.Allowed);
        Assert.Equal("allow", decision.Action);
    }

    [Fact]
    public void EvaluatePolicy_UnknownAttribute_IsIgnored()
    {
        var policy = CreatePolicy("pol-4", "identity",
            new List<PolicyCondition> { new("unknown_field", "equals", "something") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var context = CreateContext("identity", new Dictionary<string, string> { ["trust_score"] = "75" });

        var decision = _engine.EvaluatePolicy(policy, context);

        // Unknown attribute is skipped, conditions still pass, action executes
        Assert.False(decision.Allowed);
        Assert.Equal("deny", decision.Action);
    }

    [Fact]
    public void EvaluatePolicy_GreaterThanOperator_Works()
    {
        var policy = CreatePolicy("pol-5", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var passingContext = CreateContext("identity", new Dictionary<string, string> { ["trust_score"] = "75" });
        var failingContext = CreateContext("identity", new Dictionary<string, string> { ["trust_score"] = "25" });

        Assert.True(_engine.EvaluatePolicy(policy, passingContext).Allowed);
        Assert.Equal("skip", _engine.EvaluatePolicy(policy, failingContext).Action);
    }

    [Fact]
    public void EvaluatePolicy_LessThanOperator_Works()
    {
        var policy = CreatePolicy("pol-6", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "50") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var passingContext = CreateContext("identity", new Dictionary<string, string> { ["trust_score"] = "30" });
        var failingContext = CreateContext("identity", new Dictionary<string, string> { ["trust_score"] = "75" });

        Assert.False(_engine.EvaluatePolicy(policy, passingContext).Allowed);
        Assert.Equal("skip", _engine.EvaluatePolicy(policy, failingContext).Action);
    }

    [Fact]
    public void EvaluatePolicy_EqualsOperator_Works()
    {
        var policy = CreatePolicy("pol-7", "identity",
            new List<PolicyCondition> { new("region", "equals", "us-east") },
            new List<PolicyAction> { new("flag", new Dictionary<string, string>()) });

        var matchContext = CreateContext("identity", new Dictionary<string, string> { ["region"] = "us-east" });
        var noMatchContext = CreateContext("identity", new Dictionary<string, string> { ["region"] = "eu-west" });

        var matchDecision = _engine.EvaluatePolicy(policy, matchContext);
        var noMatchDecision = _engine.EvaluatePolicy(policy, noMatchContext);

        Assert.Equal("flag", matchDecision.Action);
        Assert.True(matchDecision.Allowed); // informational action
        Assert.Equal("skip", noMatchDecision.Action);
    }

    [Fact]
    public void EvaluatePolicies_MultiplePolicies_ReturnsMultipleDecisions()
    {
        var policy1 = CreatePolicy("pol-8a", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var policy2 = CreatePolicy("pol-8b", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "100") },
            new List<PolicyAction> { new("log", new Dictionary<string, string>()) });

        RegisterPolicy(policy1);
        RegisterPolicy(policy2);

        var context = CreateContext("identity", new Dictionary<string, string> { ["trust_score"] = "75" });

        var decisions = _engine.EvaluatePolicies("identity", context);

        Assert.Equal(2, decisions.Count);
    }

    [Fact]
    public void EvaluatePolicies_DependencyOrder_IsRespected()
    {
        var policyA = CreatePolicy("dep-a", "identity",
            new List<PolicyCondition>(),
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var policyB = CreatePolicy("dep-b", "identity",
            new List<PolicyCondition>(),
            new List<PolicyAction> { new("log", new Dictionary<string, string>()) });

        RegisterPolicy(policyA);
        RegisterPolicy(policyB);

        // A depends on B → B should be evaluated first
        _dependencyStore.Add(new PolicyDependency("dep-a", "dep-b"));

        var context = CreateContext("identity", new Dictionary<string, string>());

        var decisions = _engine.EvaluatePolicies("identity", context);

        Assert.Equal(2, decisions.Count);
        Assert.Equal("dep-b", decisions[0].PolicyId);
        Assert.Equal("dep-a", decisions[1].PolicyId);
    }

    [Fact]
    public void EvaluatePolicies_DomainFiltering_Works()
    {
        var identityPolicy = CreatePolicy("dom-1", "identity",
            new List<PolicyCondition>(),
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var economicPolicy = CreatePolicy("dom-2", "economic",
            new List<PolicyCondition>(),
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        RegisterPolicy(identityPolicy);
        RegisterPolicy(economicPolicy);

        var context = CreateContext("identity", new Dictionary<string, string>());

        var decisions = _engine.EvaluatePolicies("identity", context);

        Assert.Single(decisions);
        Assert.Equal("dom-1", decisions[0].PolicyId);
    }
}
