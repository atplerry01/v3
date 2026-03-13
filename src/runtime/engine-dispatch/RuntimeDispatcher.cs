namespace Whycespace.Runtime.Dispatcher;

using Whycespace.Runtime.Registry;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Runtime;

public sealed class RuntimeDispatcher : IEngineRuntimeDispatcher
{
    private readonly EngineRegistry _registry;

    public RuntimeDispatcher(EngineRegistry registry)
    {
        _registry = registry;
    }

    public async Task<EngineResult> DispatchAsync(EngineInvocationEnvelope envelope)
    {
        var engine = _registry.Resolve(envelope.EngineName);
        if (engine is null)
            return EngineResult.Fail($"Engine not found: {envelope.EngineName}");

        var context = new EngineContext(
            envelope.InvocationId,
            envelope.WorkflowId,
            envelope.WorkflowStep,
            envelope.PartitionKey,
            envelope.Context
        );

        return await engine.ExecuteAsync(context);
    }
}
