namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;
using Whycespace.System.Upstream.WhycePolicy.Stores;

public sealed class PolicyLifecycleManager
{
    private readonly PolicyLifecycleStore _store;

    public PolicyLifecycleManager(PolicyLifecycleStore store)
    {
        _store = store;
    }

    public PolicyLifecycleRecord ApprovePolicy(string policyId, string version)
    {
        return Transition(policyId, version, PolicyLifecycleState.Draft, PolicyLifecycleState.Approved);
    }

    public PolicyLifecycleRecord ActivatePolicy(string policyId, string version)
    {
        return Transition(policyId, version, PolicyLifecycleState.Approved, PolicyLifecycleState.Active);
    }

    public PolicyLifecycleRecord DeprecatePolicy(string policyId, string version)
    {
        return Transition(policyId, version, PolicyLifecycleState.Active, PolicyLifecycleState.Deprecated);
    }

    public PolicyLifecycleRecord ArchivePolicy(string policyId, string version)
    {
        return Transition(policyId, version, PolicyLifecycleState.Deprecated, PolicyLifecycleState.Archived);
    }

    public PolicyLifecycleRecord GetLifecycleState(string policyId, string version)
    {
        var record = _store.GetLifecycleState(policyId, version);
        if (record is null)
            throw new KeyNotFoundException($"No lifecycle state found for policy '{policyId}' version '{version}'.");
        return record;
    }

    public IReadOnlyList<PolicyLifecycleRecord> GetLifecycleHistory(string policyId, string version)
    {
        return _store.GetLifecycleHistory(policyId, version);
    }

    private PolicyLifecycleRecord Transition(string policyId, string version, PolicyLifecycleState requiredState, PolicyLifecycleState newState)
    {
        var current = _store.GetLifecycleState(policyId, version);

        if (current is null && requiredState != PolicyLifecycleState.Draft)
            throw new InvalidOperationException(
                $"Policy '{policyId}' version '{version}' has no lifecycle state. Expected '{requiredState}'.");

        if (current is null && requiredState == PolicyLifecycleState.Draft)
            throw new InvalidOperationException(
                $"Policy '{policyId}' version '{version}' has no lifecycle state. Register it as Draft first.");

        if (current!.State != requiredState)
            throw new InvalidOperationException(
                $"Invalid transition: policy '{policyId}' version '{version}' is in state '{current.State}', expected '{requiredState}'.");

        var record = new PolicyLifecycleRecord(policyId, version, newState, DateTime.UtcNow);
        _store.SetLifecycleState(record);
        return record;
    }
}
