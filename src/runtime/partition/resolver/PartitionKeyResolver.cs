namespace Whycespace.PartitionRuntime.Resolver;

using Whycespace.CommandSystem.Models;
using Whycespace.Contracts.Primitives;

public sealed class PartitionKeyResolver : IPartitionKeyResolver
{
    private static readonly string[] AggregateKeys =
    [
        "RiderId",
        "PropertyId",
        "SPVId"
    ];

    public PartitionKey Resolve(CommandEnvelope command)
    {
        foreach (var key in AggregateKeys)
        {
            if (command.Payload.TryGetValue(key, out var value) && value is string s && !string.IsNullOrWhiteSpace(s))
                return new PartitionKey(s);
        }

        return new PartitionKey(command.CommandId.ToString());
    }
}
