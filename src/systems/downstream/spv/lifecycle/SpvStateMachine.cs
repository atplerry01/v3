namespace Whycespace.Systems.Downstream.Spv.Lifecycle;

public sealed class SpvStateMachine
{
    private readonly Dictionary<Guid, SpvLifecycleState> _states = new();

    private static readonly Dictionary<SpvLifecycleState, HashSet<SpvLifecycleState>> ValidTransitions = new()
    {
        [SpvLifecycleState.None] = [SpvLifecycleState.Created],
        [SpvLifecycleState.Created] = [SpvLifecycleState.Active, SpvLifecycleState.Terminated],
        [SpvLifecycleState.Active] = [SpvLifecycleState.Suspended, SpvLifecycleState.Terminated],
        [SpvLifecycleState.Suspended] = [SpvLifecycleState.Active, SpvLifecycleState.Terminated],
        [SpvLifecycleState.Terminated] = []
    };

    public SpvLifecycleState GetState(Guid spvId)
    {
        _states.TryGetValue(spvId, out var state);
        return state;
    }

    public void TransitionTo(Guid spvId, SpvLifecycleState newState)
    {
        _states[spvId] = newState;
    }

    public bool TryTransition(Guid spvId, SpvLifecycleState newState)
    {
        var currentState = GetState(spvId);

        if (!ValidTransitions.TryGetValue(currentState, out var allowed))
            return false;

        if (!allowed.Contains(newState))
            return false;

        _states[spvId] = newState;
        return true;
    }
}
