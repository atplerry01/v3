namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;
using Whycespace.System.Upstream.WhycePolicy.Stores;

public sealed class PolicyRegistryEngine
{
    private readonly PolicyRegistryStore _store;

    public PolicyRegistryEngine(PolicyRegistryStore store)
    {
        _store = store;
    }

    public PolicyRecord RegisterPolicy(PolicyDefinition definition)
    {
        var existingVersion = _store.GetLatestVersion(definition.PolicyId);

        if (existingVersion > 0 && existingVersion >= definition.Version)
            throw new InvalidOperationException(
                $"Policy '{definition.PolicyId}' version {definition.Version} already exists. Latest version: {existingVersion}.");

        var record = new PolicyRecord(
            definition.PolicyId,
            definition.Version,
            definition,
            PolicyStatus.Active,
            DateTime.UtcNow
        );

        _store.Register(record);
        return record;
    }

    public PolicyRecord GetPolicy(string policyId)
    {
        var record = _store.Get(policyId);
        if (record is null)
            throw new KeyNotFoundException($"Policy not found: '{policyId}'.");
        return record;
    }

    public IReadOnlyList<PolicyRecord> GetPolicies()
    {
        return _store.GetAll();
    }
}
