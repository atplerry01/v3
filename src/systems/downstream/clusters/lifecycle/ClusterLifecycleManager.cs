namespace Whycespace.Systems.Downstream.Clusters.Lifecycle;

public sealed class ClusterLifecycleManager
{
    private readonly Dictionary<string, ClusterLifecycleState> _states = new();

    public ClusterLifecycleState GetState(string clusterId)
    {
        _states.TryGetValue(clusterId, out var state);
        return state;
    }

    public bool TryTransition(string clusterId, ClusterLifecycleState newState)
    {
        var current = GetState(clusterId);

        if (!ClusterLifecyclePolicy.IsValidTransition(current, newState))
            return false;

        _states[clusterId] = newState;
        return true;
    }

    public void Initialize(string clusterId)
    {
        _states[clusterId] = ClusterLifecycleState.Registered;
    }
}
