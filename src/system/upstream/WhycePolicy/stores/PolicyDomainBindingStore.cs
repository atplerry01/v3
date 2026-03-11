namespace Whycespace.System.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed class PolicyDomainBindingStore
{
    private readonly ConcurrentDictionary<string, List<PolicyDomainBinding>> _bindings = new();

    public void BindPolicyToDomain(PolicyDomainBinding binding)
    {
        _bindings.AddOrUpdate(
            binding.PolicyId,
            _ => new List<PolicyDomainBinding> { binding },
            (_, existing) =>
            {
                lock (existing)
                {
                    var duplicate = existing.Any(b =>
                        b.Version == binding.Version && b.Domain == binding.Domain);
                    if (duplicate)
                        throw new InvalidOperationException(
                            $"Policy '{binding.PolicyId}' version '{binding.Version}' is already bound to domain '{binding.Domain}'.");
                    existing.Add(binding);
                }
                return existing;
            });
    }

    public IReadOnlyList<PolicyDomainBinding> GetBindingsForPolicy(string policyId)
    {
        if (_bindings.TryGetValue(policyId, out var bindings))
        {
            lock (bindings)
            {
                return bindings.ToList();
            }
        }
        return Array.Empty<PolicyDomainBinding>();
    }

    public IReadOnlyList<PolicyDomainBinding> GetPoliciesForDomain(string domain)
    {
        var result = new List<PolicyDomainBinding>();
        foreach (var kvp in _bindings)
        {
            lock (kvp.Value)
            {
                result.AddRange(kvp.Value.Where(b => b.Domain == domain));
            }
        }
        return result;
    }
}
