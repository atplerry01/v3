namespace Whycespace.Systems.Downstream.Coordination;

public sealed class ExecutionToSpvBridge
{
    private readonly Dictionary<string, Guid> _clusterToSpvMappings = new();

    public void RegisterMapping(string clusterId, Guid spvId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);

        if (spvId == Guid.Empty)
            throw new ArgumentException("SpvId cannot be empty.", nameof(spvId));

        _clusterToSpvMappings[clusterId] = spvId;
    }

    public Guid? MapToSpv(string clusterId, string executionType)
    {
        if (_clusterToSpvMappings.TryGetValue(clusterId, out var spvId))
            return spvId;

        return null;
    }

    public bool HasMapping(string clusterId)
        => _clusterToSpvMappings.ContainsKey(clusterId);
}
