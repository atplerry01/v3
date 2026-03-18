namespace Whycespace.Engines.T0U.WhycePolicy.Governance.Conflict;

using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyConflictDetectionEngine
{
    private readonly PolicyRegistryStore _registryStore;
    private readonly PolicyDependencyStore _dependencyStore;

    public PolicyConflictDetectionEngine(PolicyRegistryStore registryStore, PolicyDependencyStore dependencyStore)
    {
        _registryStore = registryStore;
        _dependencyStore = dependencyStore;
    }

    public PolicyConflictReport DetectConflicts(string domain)
    {
        var policies = _registryStore.GetAll()
            .Where(r => r.Status == PolicyStatus.Active && r.PolicyDefinition.TargetDomain == domain)
            .Select(r => r.PolicyDefinition)
            .ToList();

        var conflicts = new List<PolicyConflict>();

        for (var i = 0; i < policies.Count; i++)
        {
            for (var j = i + 1; j < policies.Count; j++)
            {
                var policyA = policies[i];
                var policyB = policies[j];

                if (HasContradictingActions(policyA, policyB))
                {
                    var actionA = GetHardAction(policyA);
                    var actionB = GetHardAction(policyB);
                    conflicts.Add(new PolicyConflict(
                        policyA.PolicyId,
                        policyB.PolicyId,
                        domain,
                        $"Contradicting actions: '{policyA.PolicyId}' -> {actionA}, '{policyB.PolicyId}' -> {actionB}",
                        DateTime.UtcNow
                    ));
                }
                else if (HasDuplicateConditionsWithDifferentActions(policyA, policyB))
                {
                    conflicts.Add(new PolicyConflict(
                        policyA.PolicyId,
                        policyB.PolicyId,
                        domain,
                        $"Duplicate conditions with different actions: '{policyA.PolicyId}' vs '{policyB.PolicyId}'",
                        DateTime.UtcNow
                    ));
                }
            }
        }

        DetectDependencyChainConflicts(policies, conflicts, domain);

        return new PolicyConflictReport(domain, conflicts, DateTime.UtcNow);
    }

    private static bool HasContradictingActions(PolicyDefinition a, PolicyDefinition b)
    {
        var actionA = GetHardAction(a);
        var actionB = GetHardAction(b);

        if (actionA is null || actionB is null)
            return false;

        return (actionA == "allow" && actionB == "deny") ||
               (actionA == "deny" && actionB == "allow");
    }

    private static bool HasDuplicateConditionsWithDifferentActions(PolicyDefinition a, PolicyDefinition b)
    {
        if (a.Conditions.Count == 0 || b.Conditions.Count == 0)
            return false;

        var conditionsMatch = a.Conditions.Count == b.Conditions.Count &&
            a.Conditions.All(ca => b.Conditions.Any(cb =>
                ca.Field == cb.Field && ca.Operator == cb.Operator && ca.Value == cb.Value));

        if (!conditionsMatch)
            return false;

        var actionsA = a.Actions.Select(x => x.ActionType).OrderBy(x => x).ToList();
        var actionsB = b.Actions.Select(x => x.ActionType).OrderBy(x => x).ToList();

        return !actionsA.SequenceEqual(actionsB);
    }

    private void DetectDependencyChainConflicts(List<PolicyDefinition> policies, List<PolicyConflict> conflicts, string domain)
    {
        var policyMap = policies.ToDictionary(p => p.PolicyId);

        foreach (var policy in policies)
        {
            var deps = _dependencyStore.GetDependencies(policy.PolicyId);
            foreach (var dep in deps)
            {
                if (!policyMap.TryGetValue(dep.DependsOnPolicyId, out var depPolicy))
                    continue;

                var alreadyDetected = conflicts.Any(c =>
                    (c.PolicyA == policy.PolicyId && c.PolicyB == depPolicy.PolicyId) ||
                    (c.PolicyA == depPolicy.PolicyId && c.PolicyB == policy.PolicyId));

                if (alreadyDetected)
                    continue;

                if (HasContradictingActions(policy, depPolicy))
                {
                    conflicts.Add(new PolicyConflict(
                        policy.PolicyId,
                        depPolicy.PolicyId,
                        domain,
                        $"Dependency chain conflict: '{policy.PolicyId}' depends on '{depPolicy.PolicyId}' but they have contradicting actions",
                        DateTime.UtcNow
                    ));
                }
            }
        }
    }

    private static string? GetHardAction(PolicyDefinition policy)
    {
        return policy.Actions
            .Select(a => a.ActionType)
            .FirstOrDefault(a => a is "allow" or "deny");
    }
}
