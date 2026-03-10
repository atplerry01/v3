namespace Whycespace.Engines.T1M_Orchestration;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("WorkflowStateProjection", EngineTier.T1M, EngineKind.Projection, "WorkflowStateProjectionRequest", typeof(EngineEvent))]
public sealed class WorkflowStateProjectionEngine : IEngine
{
    public string Name => "WorkflowStateProjection";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var workflowId = context.WorkflowId;
        var currentStep = context.WorkflowStep;
        var status = context.Data.GetValueOrDefault("status") as string ?? "Running";

        var projectionId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("WorkflowStateProjected", Guid.Parse(workflowId),
                new Dictionary<string, object>
                {
                    ["projectionId"] = projectionId.ToString(),
                    ["workflowId"] = workflowId,
                    ["currentStep"] = currentStep,
                    ["status"] = status,
                    ["projectedAt"] = DateTimeOffset.UtcNow.ToString("O")
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["projectionId"] = projectionId.ToString(),
                ["workflowId"] = workflowId,
                ["currentStep"] = currentStep,
                ["status"] = status
            }));
    }
}
