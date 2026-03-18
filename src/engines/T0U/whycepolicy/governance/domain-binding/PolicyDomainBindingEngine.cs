namespace Whycespace.Engines.T0U.WhycePolicy.Governance.DomainBinding;

using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyDomainBindingEngine
{
    private const string DefaultDomain = "platform";

    private readonly PolicyDomainBindingStore _store;

    public PolicyDomainBindingEngine(PolicyDomainBindingStore store)
    {
        _store = store;
    }

    public PolicyDomainBinding BindPolicy(string policyId, string version, string domain)
    {
        if (string.IsNullOrWhiteSpace(policyId))
            throw new ArgumentException("Policy ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be empty.");

        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain cannot be empty.");

        var binding = new PolicyDomainBinding(policyId, version, domain, DateTime.UtcNow);
        _store.BindPolicyToDomain(binding);
        return binding;
    }

    public IReadOnlyList<string> GetDomainsForPolicy(string policyId)
    {
        var bindings = _store.GetBindingsForPolicy(policyId);
        if (bindings.Count == 0)
            return new[] { DefaultDomain };

        return bindings.Select(b => b.Domain).Distinct().ToList();
    }

    public IReadOnlyList<PolicyDomainBinding> GetPoliciesForDomain(string domain)
    {
        return _store.GetPoliciesForDomain(domain);
    }
}
