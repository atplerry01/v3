using Whycespace.Systems.Downstream.Clusters.Administration.Policy;
using Whycespace.Systems.Downstream.Clusters.Events;

namespace Whycespace.Systems.Downstream.Clusters.Administration;

public sealed class ClusterAdministrationManager
{
    private readonly Dictionary<string, List<ClusterAdministratorRecord>> _administrators = new();
    private readonly ClusterPolicyAdapter _policyAdapter;
    private readonly ClusterEventAdapter _eventAdapter;

    public ClusterAdministrationManager(
        ClusterPolicyAdapter policyAdapter,
        ClusterEventAdapter eventAdapter)
    {
        _policyAdapter = policyAdapter;
        _eventAdapter = eventAdapter;
    }

    public async Task<bool> AssignAdministratorAsync(string clusterId, ClusterAdministratorRecord admin, Guid assignerIdentityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);
        ArgumentNullException.ThrowIfNull(admin);

        var decision = await _policyAdapter.EvaluateAdministratorAssignmentAsync(
            assignerIdentityId, admin.IdentityId, clusterId, admin.Role);

        if (!decision.IsPermitted)
            return false;

        if (!_administrators.TryGetValue(clusterId, out var list))
        {
            list = new List<ClusterAdministratorRecord>();
            _administrators[clusterId] = list;
        }

        list.Add(admin);
        return true;
    }

    public IReadOnlyList<ClusterAdministratorRecord> GetAdministrators(string clusterId)
    {
        if (!_administrators.TryGetValue(clusterId, out var list))
            return [];

        return list;
    }

    public bool IsAdministrator(string clusterId, Guid identityId)
    {
        if (!_administrators.TryGetValue(clusterId, out var list))
            return false;

        return list.Any(a => a.IdentityId == identityId && a.Status == "Active");
    }
}
