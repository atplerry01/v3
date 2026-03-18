namespace Whycespace.Systems.Downstream.Clusters.Lifecycle;

public static class ClusterLifecyclePolicy
{
    private static readonly Dictionary<ClusterLifecycleState, HashSet<ClusterLifecycleState>> ValidTransitions = new()
    {
        [ClusterLifecycleState.None] = [ClusterLifecycleState.Registered],
        [ClusterLifecycleState.Registered] = [ClusterLifecycleState.Active, ClusterLifecycleState.Decommissioned],
        [ClusterLifecycleState.Active] = [ClusterLifecycleState.Suspended, ClusterLifecycleState.Decommissioned],
        [ClusterLifecycleState.Suspended] = [ClusterLifecycleState.Active, ClusterLifecycleState.Decommissioned],
        [ClusterLifecycleState.Decommissioned] = []
    };

    public static bool IsValidTransition(ClusterLifecycleState from, ClusterLifecycleState to)
    {
        if (!ValidTransitions.TryGetValue(from, out var allowed))
            return false;

        return allowed.Contains(to);
    }
}
