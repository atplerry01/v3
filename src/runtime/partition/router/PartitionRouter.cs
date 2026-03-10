namespace Whycespace.PartitionRuntime.Router;

using Whycespace.Contracts.Primitives;

public sealed class PartitionRouter : IPartitionRouter
{
    private readonly int _partitionCount;

    public PartitionRouter(int partitionCount = 16)
    {
        if (partitionCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(partitionCount), "Partition count must be greater than zero.");

        _partitionCount = partitionCount;
    }

    public int PartitionCount => _partitionCount;

    public int ResolvePartition(PartitionKey key)
    {
        return Math.Abs(key.Value.GetHashCode()) % _partitionCount;
    }
}
