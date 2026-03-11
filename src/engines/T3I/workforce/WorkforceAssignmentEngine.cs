namespace Whycespace.Engines.T3I_Intelligence;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("WorkforceAssignment", EngineTier.T3I, EngineKind.Decision, "WorkforceAssignmentRequest", typeof(EngineEvent))]
public sealed class WorkforceAssignmentEngine : IEngine
{
    public string Name => "WorkforceAssignment";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var taskType = context.Data.GetValueOrDefault("taskType") as string ?? "general";
        var assigneeId = Guid.NewGuid().ToString();

        var events = new[]
        {
            EngineEvent.Create("WorkforceAssigned", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["assigneeId"] = assigneeId,
                    ["taskType"] = taskType
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object> { ["assigneeId"] = assigneeId }));
    }
}
