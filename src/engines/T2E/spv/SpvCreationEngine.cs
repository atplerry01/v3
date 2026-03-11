namespace Whycespace.Engines.T2E_Execution;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("SpvCreation", EngineTier.T2E, EngineKind.Mutation, "SpvCreationRequest", typeof(EngineEvent))]
public sealed class SpvCreationEngine : IEngine
{
    public string Name => "SpvCreation";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var spvName = context.Data.GetValueOrDefault("spvName") as string;
        if (string.IsNullOrEmpty(spvName))
            return Task.FromResult(EngineResult.Fail("Missing spvName"));

        var capitalId = context.Data.GetValueOrDefault("capitalId") as string;
        if (string.IsNullOrEmpty(capitalId))
            return Task.FromResult(EngineResult.Fail("Missing capitalId"));

        if (!Guid.TryParse(capitalId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid capitalId format"));

        var spvId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("SpvCreated", spvId,
                new Dictionary<string, object>
                {
                    ["spvId"] = spvId.ToString(),
                    ["spvName"] = spvName,
                    ["capitalId"] = capitalId,
                    ["topic"] = "whyce.spv.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["spvId"] = spvId.ToString(),
                ["spvName"] = spvName,
                ["capitalId"] = capitalId
            }));
    }
}
