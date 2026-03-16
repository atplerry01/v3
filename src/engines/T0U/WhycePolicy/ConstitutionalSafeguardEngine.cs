namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class ConstitutionalSafeguardEngine
{
    private readonly ConstitutionalPolicyStore _store;

    public ConstitutionalSafeguardEngine(ConstitutionalPolicyStore store)
    {
        _store = store;
    }

    public ConstitutionalPolicyRecord RegisterConstitutionalPolicy(string policyId, string version, string protectionLevel)
    {
        var record = new ConstitutionalPolicyRecord(policyId, version, protectionLevel, DateTime.UtcNow);
        _store.RegisterConstitutionalPolicy(record);
        return record;
    }

    public void ValidatePolicyModification(string policyId, string version)
    {
        var level = _store.GetProtectionLevel(policyId, version);
        if (level == "Immutable")
            throw new InvalidOperationException(
                $"Policy '{policyId}' version '{version}' is immutable and cannot be modified.");
    }

    public void ValidatePolicyDeletion(string policyId, string version)
    {
        var level = _store.GetProtectionLevel(policyId, version);
        if (level == "Immutable")
            throw new InvalidOperationException(
                $"Policy '{policyId}' version '{version}' is immutable and cannot be deleted.");
    }

    public void ValidatePolicyActivation(string policyId, string version)
    {
        var level = _store.GetProtectionLevel(policyId, version);
        if (level == "SystemCritical")
            throw new InvalidOperationException(
                $"Policy '{policyId}' version '{version}' is system-critical and cannot be disabled.");
    }
}
