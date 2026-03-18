namespace Whycespace.Platform.Simulation.Dispatch;

using Whycespace.Contracts.Engines;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Runtime;
using Whycespace.EngineRuntime.Registry;

public sealed class SimulationEngineDispatcher : IEngineRuntimeDispatcher
{
    private readonly EngineRegistry _registry;

    public SimulationEngineDispatcher(EngineRegistry registry)
    {
        _registry = registry;
    }

    public async Task<EngineResult> DispatchAsync(EngineInvocationEnvelope envelope)
    {
        var engine = _registry.Resolve(envelope.EngineName);
        var context = new EngineContext(
            envelope.InvocationId,
            envelope.WorkflowId,
            envelope.WorkflowStep,
            envelope.PartitionKey,
            envelope.Context);
        return await engine.ExecuteAsync(context);
    }
}