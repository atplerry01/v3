namespace Whycespace.Engines.T1M_Orchestration;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("PartitionRouter", EngineTier.T1M, EngineKind.Decision, "PartitionRouterRequest", typeof(EngineEvent))]
public sealed class PartitionRouterEngine : IEngine
{
    public string Name => "PartitionRouter";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var partitionKey = context.PartitionKey;
        var targetPartition = ResolvePartition(partitionKey);

        var events = new[]
        {
            EngineEvent.Create("PartitionRouted", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["partitionKey"] = partitionKey,
                    ["targetPartition"] = targetPartition
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object> { ["partition"] = targetPartition }));
    }

    private static string ResolvePartition(string partitionKey)
    {
        var hash = partitionKey.GetHashCode() & 0x7FFFFFFF;
        return $"partition-{hash % 16}";
    }
}
