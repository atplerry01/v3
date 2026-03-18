namespace Whycespace.Contracts.Tests;

using Whycespace.Shared.Primitives.Common;

public sealed class PartitionKeyTests
{
    [Fact]
    public void CreatePartitionKey()
    {
        var key = new PartitionKey("partition-1");
        Assert.Equal("partition-1", key.Value);
    }

    [Fact]
    public void EmptyPartitionKey()
    {
        var key = PartitionKey.Empty;
        Assert.True(key.IsEmpty);
        Assert.Equal("", key.Value);
    }

    [Fact]
    public void ToStringReturnsValue()
    {
        var key = new PartitionKey("my-partition");
        Assert.Equal("my-partition", key.ToString());
    }

    [Fact]
    public void EqualityComparison()
    {
        var key1 = new PartitionKey("same");
        var key2 = new PartitionKey("same");
        var key3 = new PartitionKey("different");

        Assert.Equal(key1, key2);
        Assert.NotEqual(key1, key3);
    }
}
