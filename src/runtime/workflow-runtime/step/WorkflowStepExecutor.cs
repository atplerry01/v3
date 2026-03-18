namespace Whycespace.WorkflowRuntime.Step;

using Whycespace.Contracts.Engines;
using Whycespace.Shared.Envelopes;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Contracts.Workflows;

public sealed class WorkflowStepExecutor
{
    private readonly Func<EngineInvocationEnvelope, Task<EngineResult>> _dispatchFunc;

    public WorkflowStepExecutor(Func<EngineInvocationEnvelope, Task<EngineResult>> dispatchFunc)
    {
        _dispatchFunc = dispatchFunc;
    }

    public async Task<EngineResult> ExecuteStepAsync(
        WorkflowStep step,
        string workflowId,
        PartitionKey partitionKey,
        IReadOnlyDictionary<string, object> context)
    {
        var envelope = new EngineInvocationEnvelope(
            InvocationId: Guid.NewGuid(),
            EngineName: step.EngineName,
            WorkflowId: workflowId,
            WorkflowStep: step.StepId,
            PartitionKey: partitionKey,
            Context: context);

        return await _dispatchFunc(envelope);
    }
}
