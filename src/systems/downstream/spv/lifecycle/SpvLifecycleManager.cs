using Whycespace.Systems.Downstream.Spv.Events;
using Whycespace.Systems.Downstream.Spv.Governance.Policy;

namespace Whycespace.Systems.Downstream.Spv.Lifecycle;

public sealed class SpvLifecycleManager
{
    private readonly SpvStateMachine _stateMachine;
    private readonly SpvCreationPolicy _creationPolicy;
    private readonly SpvTerminationPolicy _terminationPolicy;
    private readonly SpvPolicyAdapter _policyAdapter;
    private readonly SpvEventAdapter _eventAdapter;

    public SpvLifecycleManager(
        SpvStateMachine stateMachine,
        SpvCreationPolicy creationPolicy,
        SpvTerminationPolicy terminationPolicy,
        SpvPolicyAdapter policyAdapter,
        SpvEventAdapter eventAdapter)
    {
        _stateMachine = stateMachine;
        _creationPolicy = creationPolicy;
        _terminationPolicy = terminationPolicy;
        _policyAdapter = policyAdapter;
        _eventAdapter = eventAdapter;
    }

    public SpvLifecycleState GetCurrentState(Guid spvId)
        => _stateMachine.GetState(spvId);

    public async Task<bool> TryCreateAsync(Guid spvId, string name, string clusterId, decimal allocatedCapital, Guid initiatorId)
    {
        var decision = await _policyAdapter.EvaluateSpvCreationAsync(clusterId, allocatedCapital, initiatorId);

        if (!decision.IsPermitted)
            return false;

        if (!_creationPolicy.CanCreate(clusterId, allocatedCapital))
            return false;

        _stateMachine.TransitionTo(spvId, SpvLifecycleState.Created);

        await _eventAdapter.PublishSpvCreatedAsync(spvId, name, clusterId, allocatedCapital);
        await _eventAdapter.PublishLifecycleTransitionAsync(spvId, "None", "Created", "Initial creation");

        return true;
    }

    public async Task<bool> TryActivateAsync(Guid spvId, Guid initiatorId)
    {
        var currentState = _stateMachine.GetState(spvId);
        var decision = await _policyAdapter.EvaluateLifecycleTransitionAsync(spvId, currentState.ToString(), "Active", initiatorId);

        if (!decision.IsPermitted)
            return false;

        var result = _stateMachine.TryTransition(spvId, SpvLifecycleState.Active);

        if (result)
            await _eventAdapter.PublishLifecycleTransitionAsync(spvId, currentState.ToString(), "Active", "Activation");

        return result;
    }

    public bool TrySuspend(Guid spvId)
        => _stateMachine.TryTransition(spvId, SpvLifecycleState.Suspended);

    public bool TryTerminate(Guid spvId, string reason)
    {
        if (!_terminationPolicy.CanTerminate(spvId, reason))
            return false;

        return _stateMachine.TryTransition(spvId, SpvLifecycleState.Terminated);
    }
}

public enum SpvLifecycleState
{
    None,
    Created,
    Active,
    Suspended,
    Terminated
}
