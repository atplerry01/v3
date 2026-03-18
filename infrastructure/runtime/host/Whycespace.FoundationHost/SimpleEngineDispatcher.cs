namespace Whycespace.FoundationHost;

using Whycespace.Contracts.Engines;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Runtime;
using Whycespace.EngineRuntime.Registry;

/// <summary>
/// Simple engine dispatcher that resolves an engine from the registry by name
/// and invokes it. Replaces the former Whycespace.Runtime.Dispatcher.RuntimeDispatcher
/// that was removed during the engine-layer refactor.
/// </summary>
public sealed class SimpleEngineDispatcher : IEngineRuntimeDispatcher
{
    private readonly EngineRegistry _registry;

    public SimpleEngineDispatcher(EngineRegistry registry)
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
