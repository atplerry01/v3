namespace Whycespace.PartitionRuntime.Tests;

using Whycespace.Shared.Primitives.Common;
using Whycespace.PartitionRuntime.Router;

public class PartitionRouterTests
{
    [Fact]
    public void ResolvePartition_ReturnsDeterministicPartition()
    {
        var router = new PartitionRouter(16);
        var key = new PartitionKey("rider-42");

        var partition1 = router.ResolvePartition(key);
        var partition2 = router.ResolvePartition(key);

        Assert.Equal(partition1, partition2);
    }

    [Fact]
    public void ResolvePartition_ReturnsWithinRange()
    {
        var router = new PartitionRouter(16);
        var key = new PartitionKey("rider-42");

        var partition = router.ResolvePartition(key);

        Assert.InRange(partition, 0, 15);
    }

    [Fact]
    public void ResolvePartition_DifferentKeysCanMapToDifferentPartitions()
    {
        var router = new PartitionRouter(16);
        var partitions = new HashSet<int>();

        for (int i = 0; i < 100; i++)
        {
            partitions.Add(router.ResolvePartition(new PartitionKey($"key-{i}")));
        }

        Assert.True(partitions.Count > 1, "Expected multiple partitions for different keys");
    }

    [Fact]
    public void Constructor_InvalidPartitionCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PartitionRouter(0));
    }
}
