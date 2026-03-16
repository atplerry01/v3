namespace Whycespace.Runtime.Dispatcher;

using Whycespace.EngineRuntime.Registry;
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
        try
        {
            var engine = _registry.Resolve(envelope.EngineName);

            var context = new EngineContext(
                envelope.InvocationId,
                envelope.WorkflowId,
                envelope.WorkflowStep,
                envelope.PartitionKey,
                envelope.Context
            );

            return await engine.ExecuteAsync(context);
        }
        catch (InvalidOperationException ex)
        {
            return EngineResult.Fail(ex.Message);
        }
    }
}
