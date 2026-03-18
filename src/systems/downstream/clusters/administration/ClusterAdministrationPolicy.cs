namespace Whycespace.Systems.Downstream.Clusters.Administration;

public sealed class ClusterAdministrationPolicy
{
    public bool CanAssign(string role, Guid assignerIdentityId)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        if (assignerIdentityId == Guid.Empty)
            return false;

        return true;
    }

    public bool CanRemove(string clusterId, Guid targetIdentityId, Guid removerIdentityId)
    {
        if (string.IsNullOrWhiteSpace(clusterId))
            return false;

        if (targetIdentityId == removerIdentityId)
            return false;

        return true;
    }
}
