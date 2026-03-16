namespace Whycespace.Runtime.Partition;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("PartitionRouter", EngineTier.T1M, EngineKind.Decision, "PartitionRouterRequest", typeof(EngineEvent))]
public sealed class WorkflowPartitionRouterEngine : IEngine
{
    public string Name => "PartitionRouter";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var partitionKey = context.PartitionKey;
        var resolvedPartition = ResolvePartition(partitionKey);

        var events = new[]
        {
            EngineEvent.Create("PartitionRouted", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["partitionKey"] = partitionKey,
                    ["resolvedPartition"] = resolvedPartition,
                    ["routedAt"] = DateTimeOffset.UtcNow.ToString("O")
                })
        };

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["partitionKey"] = partitionKey,
            ["resolvedPartition"] = resolvedPartition
        }));
    }

    private static int ResolvePartition(string partitionKey)
    {
        var hash = partitionKey.GetHashCode();
        return (hash & 0x7FFFFFFF) % 16;
    }
}
