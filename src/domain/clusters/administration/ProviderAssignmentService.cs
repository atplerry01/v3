namespace Whycespace.Domain.Clusters;

public sealed class ProviderAssignmentService
{
    private readonly Dictionary<string, List<Guid>> _assignments = new();

    public void AssignProviderToSubCluster(Guid providerId, string subClusterName)
    {
        if (!_assignments.TryGetValue(subClusterName, out var list))
        {
            list = new List<Guid>();
            _assignments[subClusterName] = list;
        }

        if (!list.Contains(providerId))
            list.Add(providerId);
    }

    public IReadOnlyList<Guid> GetProvidersForSubCluster(string subClusterName)
    {
        return _assignments.TryGetValue(subClusterName, out var list)
            ? list.AsReadOnly()
            : Array.Empty<Guid>();
    }

    public IReadOnlyDictionary<string, List<Guid>> GetAllAssignments()
    {
        return _assignments.AsReadOnly();
    }
}
