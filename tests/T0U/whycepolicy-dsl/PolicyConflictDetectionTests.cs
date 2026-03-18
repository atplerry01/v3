using Whycespace.Engines.T0U.WhycePolicy.Governance.Conflict;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyConflictDetectionTests
{
    private readonly PolicyRegistryStore _registryStore = new();
    private readonly PolicyDependencyStore _dependencyStore = new();
    private readonly PolicyConflictDetectionEngine _engine;

    public PolicyConflictDetectionTests()
    {
        _engine = new PolicyConflictDetectionEngine(_registryStore, _dependencyStore);
    }

    private void RegisterPolicy(string id, string domain, List<PolicyCondition> conditions, List<PolicyAction> actions)
    {
        var definition = new PolicyDefinition(id, $"Policy {id}", 1, domain, conditions, actions, DateTime.UtcNow);
        var record = new PolicyRecord(id, 1, definition, PolicyStatus.Active, DateTime.UtcNow);
        _registryStore.Register(record);
    }

    [Fact]
    public void DetectConflicts_AllowVsDeny_DetectsConflict()
    {
        RegisterPolicy("conflict-allow", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy("conflict-deny", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "80") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var report = _engine.DetectConflicts("identity");

        Assert.Single(report.Conflicts);
        Assert.Contains("Contradicting actions", report.Conflicts[0].Reason);
    }

    [Fact]
    public void DetectConflicts_DuplicateConditionsDifferentActions_DetectsConflict()
    {
        var conditions = new List<PolicyCondition> { new("trust_score", "less_than", "50") };

        RegisterPolicy("dup-allow", "identity", conditions,
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy("dup-deny", "identity", conditions,
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var report = _engine.DetectConflicts("identity");

        Assert.True(report.Conflicts.Count >= 1);
    }

    [Fact]
    public void DetectConflicts_CompatiblePolicies_NoConflict()
    {
        RegisterPolicy("compat-1", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy("compat-2", "identity",
            new List<PolicyCondition> { new("region", "equals", "us") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var report = _engine.DetectConflicts("identity");

        Assert.Empty(report.Conflicts);
    }

    [Fact]
    public void DetectConflicts_DomainFiltering_Works()
    {
        RegisterPolicy("dom-allow", "identity",
            new List<PolicyCondition>(),
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy("dom-deny", "economic",
            new List<PolicyCondition>(),
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var report = _engine.DetectConflicts("identity");

        Assert.Empty(report.Conflicts);
        Assert.Equal("identity", report.Domain);
    }

    [Fact]
    public void DetectConflicts_MultipleConflicts_AllDetected()
    {
        RegisterPolicy("multi-a", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy("multi-b", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "80") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        RegisterPolicy("multi-c", "identity",
            new List<PolicyCondition> { new("region", "equals", "eu") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var report = _engine.DetectConflicts("identity");

        // a vs b, a vs c (allow vs deny)
        Assert.Equal(2, report.Conflicts.Count);
    }

    [Fact]
    public void DetectConflicts_ReportTimestampGenerated()
    {
        var before = DateTime.UtcNow;

        var report = _engine.DetectConflicts("identity");

        Assert.True(report.GeneratedAt >= before);
        Assert.True(report.GeneratedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void DetectConflicts_LoggingOnlyPolicies_NoConflict()
    {
        RegisterPolicy("log-1", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("log", new Dictionary<string, string>()) });

        RegisterPolicy("log-2", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "80") },
            new List<PolicyAction> { new("notify", new Dictionary<string, string>()) });

        var report = _engine.DetectConflicts("identity");

        Assert.Empty(report.Conflicts);
    }

    [Fact]
    public void DetectConflicts_DependencyChainConflict_Detected()
    {
        RegisterPolicy("dep-parent", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy("dep-child", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "80") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        _dependencyStore.Add(new PolicyDependency("dep-parent", "dep-child"));

        var report = _engine.DetectConflicts("identity");

        Assert.True(report.Conflicts.Count >= 1);
        Assert.True(report.Conflicts.Any(c => c.Reason.Contains("Contradicting actions") || c.Reason.Contains("Dependency chain")));
    }
}
