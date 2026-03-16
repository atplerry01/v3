namespace Whycespace.Runtime.Dispatcher.Workflow;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("RuntimeDispatcher", EngineTier.T1M, EngineKind.Decision, "RuntimeDispatcherRequest", typeof(EngineEvent))]
public sealed class RuntimeDispatcherEngine : IEngine
{
    public string Name => "RuntimeDispatcher";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var targetEngine = context.Data.GetValueOrDefault("targetEngine") as string
            ?? throw new InvalidOperationException("Missing 'targetEngine' in context.");

        var targetStep = context.Data.GetValueOrDefault("targetStep") as string
            ?? context.WorkflowStep;

        var events = new[]
        {
            EngineEvent.Create("EngineDispatched", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["targetEngine"] = targetEngine,
                    ["targetStep"] = targetStep,
                    ["dispatchedAt"] = DateTimeOffset.UtcNow.ToString("O")
                })
        };

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["targetEngine"] = targetEngine,
            ["targetStep"] = targetStep,
            ["dispatched"] = true
        }));
    }
}
