namespace Whycespace.Engines.T2E_Execution;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("ClusterCreation", EngineTier.T2E, EngineKind.Mutation, "ClusterCreationRequest", typeof(EngineEvent))]
public sealed class ClusterCreationEngine : IEngine
{
    public string Name => "ClusterCreation";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var clusterName = context.Data.GetValueOrDefault("clusterName") as string;
        if (string.IsNullOrEmpty(clusterName))
            return Task.FromResult(EngineResult.Fail("Missing clusterName"));

        var region = context.Data.GetValueOrDefault("region") as string ?? "default";
        var clusterType = context.Data.GetValueOrDefault("clusterType") as string ?? "Mixed";

        var clusterId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("ClusterCreated", clusterId,
                new Dictionary<string, object>
                {
                    ["clusterId"] = clusterId.ToString(),
                    ["clusterName"] = clusterName,
                    ["region"] = region,
                    ["clusterType"] = clusterType,
                    ["topic"] = "whyce.cluster.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["clusterId"] = clusterId.ToString(),
                ["clusterName"] = clusterName,
                ["region"] = region,
                ["clusterType"] = clusterType
            }));
    }
}
