using Whycespace.Systems.Downstream.Spv.Lifecycle;
using Whycespace.Systems.Downstream.Spv.Registry;
using Whycespace.Systems.Downstream.Spv.Capital;
using Whycespace.Systems.Downstream.Spv.Events;
using Whycespace.Systems.Downstream.Spv.Governance.Policy;

namespace Whycespace.Systems.Downstream.Spv.Orchestration;

public sealed class SpvOrchestrator
{
    private readonly SpvRegistry _registry;
    private readonly SpvLifecycleManager _lifecycleManager;
    private readonly SpvCapitalStructure _capitalStructure;
    private readonly SpvEventAdapter _eventAdapter;
    private readonly SpvPolicyAdapter _policyAdapter;

    public SpvOrchestrator(
        SpvRegistry registry,
        SpvLifecycleManager lifecycleManager,
        SpvCapitalStructure capitalStructure,
        SpvEventAdapter eventAdapter,
        SpvPolicyAdapter policyAdapter)
    {
        _registry = registry;
        _lifecycleManager = lifecycleManager;
        _capitalStructure = capitalStructure;
        _eventAdapter = eventAdapter;
        _policyAdapter = policyAdapter;
    }

    public async Task<SpvRegistryRecord?> CreateSpvAsync(string name, string clusterId, decimal allocatedCapital, Guid initiatorId)
    {
        var spvId = Guid.NewGuid();

        if (!await _lifecycleManager.TryCreateAsync(spvId, name, clusterId, allocatedCapital, initiatorId))
            return null;

        var record = new SpvRegistryRecord(
            spvId, name, clusterId, allocatedCapital,
            "Created", DateTimeOffset.UtcNow
        );

        _registry.Register(record);
        return record;
    }

    public async Task<bool> ActivateSpvAsync(Guid spvId, Guid initiatorId)
        => await _lifecycleManager.TryActivateAsync(spvId, initiatorId);

    public async Task<bool> AllocateCapitalAsync(Guid spvId, InvestorAllocationModel allocation, Guid initiatorId)
    {
        var decision = await _policyAdapter.EvaluateCapitalAllocationAsync(spvId, allocation.InvestorIdentityId, allocation.AllocationPercentage);

        if (!decision.IsPermitted)
            return false;

        _capitalStructure.AddAllocation(spvId, allocation);

        await _eventAdapter.PublishCapitalAllocatedAsync(
            spvId, allocation.InvestorIdentityId,
            allocation.AllocationPercentage, allocation.InvestedAmount,
            allocation.AllocationClass);

        return true;
    }

    public IReadOnlyList<SpvRegistryRecord> GetSpvsByCluster(string clusterId)
        => _registry.GetByCluster(clusterId);
}
