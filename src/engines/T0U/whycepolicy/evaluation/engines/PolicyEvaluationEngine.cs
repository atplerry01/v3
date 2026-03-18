namespace Whycespace.Engines.T0U.WhycePolicy.Evaluation.Engines;

using Whycespace.Engines.T0U.WhycePolicy.Governance.Conflict;
using Whycespace.Engines.T0U.WhycePolicy.Governance.Dependency;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyEvaluationEngine
{
    private readonly PolicyRegistryStore _registryStore;
    private readonly PolicyDependencyStore _dependencyStore;

    public PolicyEvaluationEngine(PolicyRegistryStore registryStore, PolicyDependencyStore dependencyStore)
    {
        _registryStore = registryStore;
        _dependencyStore = dependencyStore;
    }

    public PolicyDecision EvaluatePolicy(PolicyDefinition policy, PolicyContext context)
    {
        if (!EvaluateConditions(policy.Conditions, context.Attributes))
        {
            return new PolicyDecision(
                policy.PolicyId,
                true,
                "skip",
                "Conditions not met",
                DateTime.UtcNow
            );
        }

        var action = policy.Actions.Count > 0 ? policy.Actions[0].ActionType : "allow";
        var allowed = ResolveAllowed(action);
        var reason = $"Policy '{policy.Name}' evaluated: {action}";

        return new PolicyDecision(
            policy.PolicyId,
            allowed,
            action,
            reason,
            DateTime.UtcNow
        );
    }

    public IReadOnlyList<PolicyDecision> EvaluatePolicies(string domain, PolicyContext context)
    {
        var allRecords = _registryStore.GetAll()
            .Where(r => r.Status == PolicyStatus.Active && r.PolicyDefinition.TargetDomain == domain)
            .ToList();

        var dependencyEngine = new PolicyDependencyEngine(_dependencyStore);
        var ordered = OrderByDependencies(allRecords, dependencyEngine);

        var decisions = new List<PolicyDecision>();
        foreach (var record in ordered)
        {
            decisions.Add(EvaluatePolicy(record.PolicyDefinition, context));
        }

        return decisions;
    }

    private static bool EvaluateConditions(IReadOnlyList<PolicyCondition> conditions, IReadOnlyDictionary<string, string> attributes)
    {
        foreach (var condition in conditions)
        {
            if (!attributes.TryGetValue(condition.Field, out var value))
                continue;

            if (!EvaluateCondition(condition.Operator, value, condition.Value))
                return false;
        }

        return true;
    }

    private static bool EvaluateCondition(string op, string actual, string expected)
    {
        return op switch
        {
            "equals" => string.Equals(actual, expected, StringComparison.Ordinal),
            "not_equals" => !string.Equals(actual, expected, StringComparison.Ordinal),
            "greater_than" => double.TryParse(actual, out var a) && double.TryParse(expected, out var b) && a > b,
            "less_than" => double.TryParse(actual, out var la) && double.TryParse(expected, out var lb) && la < lb,
            "contains" => actual.Contains(expected, StringComparison.Ordinal),
            _ => false
        };
    }

    private static bool ResolveAllowed(string action)
    {
        return action switch
        {
            "deny" => false,
            "allow" => true,
            _ => true // log, notify, flag, escalate are informational
        };
    }

    private static List<PolicyRecord> OrderByDependencies(List<PolicyRecord> records, PolicyDependencyEngine dependencyEngine)
    {
        var policyMap = records.ToDictionary(r => r.PolicyId);
        var allIds = records.Select(r => r.PolicyId).ToHashSet();

        var visited = new HashSet<string>();
        var ordered = new List<string>();

        foreach (var id in allIds)
        {
            ResolveDependencyOrder(id, allIds, dependencyEngine, visited, ordered);
        }

        return ordered
            .Where(policyMap.ContainsKey)
            .Select(id => policyMap[id])
            .ToList();
    }

    private static void ResolveDependencyOrder(string policyId, HashSet<string> available, PolicyDependencyEngine dependencyEngine, HashSet<string> visited, List<string> ordered)
    {
        if (!visited.Add(policyId))
            return;

        var deps = dependencyEngine.GetDependencies(policyId);
        foreach (var dep in deps)
        {
            if (available.Contains(dep.DependsOnPolicyId))
            {
                ResolveDependencyOrder(dep.DependsOnPolicyId, available, dependencyEngine, visited, ordered);
            }
        }

        ordered.Add(policyId);
    }
}
