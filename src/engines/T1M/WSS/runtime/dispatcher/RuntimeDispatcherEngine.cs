namespace Whycespace.Engines.T1M.WSS.Runtime.Dispatcher;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("RuntimeDispatcher", EngineTier.T1M, EngineKind.Decision, "RuntimeDispatcherRequest", typeof(EngineEvent))]
public sealed class RuntimeDispatcherEngine : IEngine
{
    public string Name => "RuntimeDispatcher";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var targetEngine = context.Data.GetValueOrDefault("targetEngine") as string;
        if (string.IsNullOrEmpty(targetEngine))
            return Task.FromResult(EngineResult.Fail("Missing targetEngine"));

        var targetStep = context.Data.GetValueOrDefault("targetStep") as string ?? context.WorkflowStep;

        var events = new[]
        {
            EngineEvent.Create("EngineDispatched", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["targetEngine"] = targetEngine,
                    ["targetStep"] = targetStep,
                    ["dispatchedAt"] = DateTimeOffset.UtcNow.ToString("O"),
                    ["invocationId"] = context.InvocationId.ToString()
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["targetEngine"] = targetEngine,
                ["targetStep"] = targetStep,
                ["dispatched"] = true
            }));
    }
}
