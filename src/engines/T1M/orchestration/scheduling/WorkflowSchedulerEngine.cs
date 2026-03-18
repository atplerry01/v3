namespace Whycespace.Engines.T1M.Orchestration.Scheduling;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("WorkflowScheduler", EngineTier.T1M, EngineKind.Decision, "WorkflowSchedulerRequest", typeof(EngineEvent))]
public sealed class WorkflowSchedulerEngine : IEngine
{
    public string Name => "WorkflowScheduler";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var workflowName = context.Data.GetValueOrDefault("workflowName") as string ?? "unknown";
        var priority = context.Data.GetValueOrDefault("priority") as string ?? "normal";

        var events = new[]
        {
            EngineEvent.Create("WorkflowScheduled", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["workflowName"] = workflowName,
                    ["priority"] = priority,
                    ["scheduledAt"] = DateTimeOffset.UtcNow.ToString("O")
                })
        };

        return Task.FromResult(EngineResult.Ok(events));
    }
}
