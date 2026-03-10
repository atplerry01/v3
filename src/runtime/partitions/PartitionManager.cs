namespace Whycespace.Runtime.Partitions;

public sealed class PartitionManager
{
    private readonly Dictionary<string, List<string>> _partitions = new();

    public void AssignToPartition(string partitionKey, string workflowId)
    {
        if (!_partitions.ContainsKey(partitionKey))
            _partitions[partitionKey] = new List<string>();
        _partitions[partitionKey].Add(workflowId);
    }

    public IReadOnlyList<string> GetWorkflowsInPartition(string partitionKey)
    {
        return _partitions.TryGetValue(partitionKey, out var workflows)
            ? workflows
            : Array.Empty<string>();
    }

    public IReadOnlyList<string> GetPartitions() => _partitions.Keys.ToList();
}
