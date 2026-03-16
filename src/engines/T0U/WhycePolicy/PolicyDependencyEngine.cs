namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyDependencyEngine
{
    private readonly PolicyDependencyStore _store;

    public PolicyDependencyEngine(PolicyDependencyStore store)
    {
        _store = store;
    }

    public PolicyDependency RegisterDependency(string policyId, string dependsOnPolicyId)
    {
        if (string.IsNullOrWhiteSpace(policyId))
            throw new ArgumentException("Policy ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(dependsOnPolicyId))
            throw new ArgumentException("Depends-on policy ID cannot be empty.");

        if (policyId == dependsOnPolicyId)
            throw new InvalidOperationException("A policy cannot depend on itself.");

        if (_store.HasDependency(policyId, dependsOnPolicyId))
            throw new InvalidOperationException(
                $"Dependency already exists: '{policyId}' -> '{dependsOnPolicyId}'.");

        if (WouldCreateCycle(policyId, dependsOnPolicyId))
            throw new InvalidOperationException(
                $"Circular dependency detected: adding '{policyId}' -> '{dependsOnPolicyId}' would create a cycle.");

        var dependency = new PolicyDependency(policyId, dependsOnPolicyId);
        _store.Add(dependency);
        return dependency;
    }

    public IReadOnlyList<string> ResolveDependencyGraph(string policyId)
    {
        var resolved = new List<string>();
        var visited = new HashSet<string>();
        Resolve(policyId, visited, resolved);
        return resolved;
    }

    public IReadOnlyList<PolicyDependency> GetDependencies(string policyId)
    {
        return _store.GetDependencies(policyId);
    }

    public bool HasCircularDependency(string policyId, string dependsOnPolicyId)
    {
        if (policyId == dependsOnPolicyId)
            return true;

        return WouldCreateCycle(policyId, dependsOnPolicyId);
    }

    private bool WouldCreateCycle(string policyId, string dependsOnPolicyId)
    {
        var visited = new HashSet<string>();
        return CanReach(dependsOnPolicyId, policyId, visited);
    }

    private bool CanReach(string from, string target, HashSet<string> visited)
    {
        if (from == target)
            return true;

        if (!visited.Add(from))
            return false;

        var deps = _store.GetDependencies(from);
        foreach (var dep in deps)
        {
            if (CanReach(dep.DependsOnPolicyId, target, visited))
                return true;
        }

        return false;
    }

    private void Resolve(string policyId, HashSet<string> visited, List<string> resolved)
    {
        if (!visited.Add(policyId))
            return;

        var deps = _store.GetDependencies(policyId);
        foreach (var dep in deps)
        {
            Resolve(dep.DependsOnPolicyId, visited, resolved);
        }

        resolved.Add(policyId);
    }
}
