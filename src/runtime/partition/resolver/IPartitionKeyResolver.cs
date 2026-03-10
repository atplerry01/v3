namespace Whycespace.PartitionRuntime.Resolver;

using Whycespace.CommandSystem.Models;
using Whycespace.Contracts.Primitives;

public interface IPartitionKeyResolver
{
    PartitionKey Resolve(CommandEnvelope command);
}
