namespace Whycespace.PartitionRuntime.Resolver;

using Whycespace.CommandSystem.Core.Models;
using Whycespace.Shared.Primitives.Common;

public interface IPartitionKeyResolver
{
    PartitionKey Resolve(CommandEnvelope command);
}
