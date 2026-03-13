namespace Whycespace.EngineRuntime.Executor;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Contracts.Workflows;
using Whycespace.EngineRuntime.Invocation;
using Whycespace.EngineRuntime.Resolver;

public sealed class WorkflowStepEngineExecutor
{
    private readonly EngineResolver _resolver;
    private readonly EngineInvocationManager _invocation;

    public WorkflowStepEngineExecutor(
        EngineResolver resolver,
        EngineInvocationManager invocation)
    {
        _resolver = resolver;
        _invocation = invocation;
    }

    public async Task<EngineResult> ExecuteStepAsync(
        WorkflowStep step,
        string workflowId,
        PartitionKey partitionKey,
        IReadOnlyDictionary<string, object> context)
    {
        var engine = _resolver.Resolve(step.EngineName);

        var engineContext = new EngineContext(
            Guid.NewGuid(),
            workflowId,
            step.StepId,
            partitionKey,
            context
        );

        return await _invocation.InvokeAsync(engine, engineContext);
    }
}
