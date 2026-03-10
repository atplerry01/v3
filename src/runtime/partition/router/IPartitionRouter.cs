namespace Whycespace.PartitionRuntime.Router;

using Whycespace.Contracts.Primitives;

public interface IPartitionRouter
{
    int ResolvePartition(PartitionKey key);
}
