namespace Whycespace.PartitionRuntime.Router;

using Whycespace.Shared.Primitives.Common;

public interface IPartitionRouter
{
    int ResolvePartition(PartitionKey key);
}
