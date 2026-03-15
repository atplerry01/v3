namespace Whycespace.WorkflowRuntime.Dispatcher;

public sealed record WorkflowDispatchCommand(
    Guid WorkflowInstanceId,
    string StepId,
    string EngineName,
    IReadOnlyDictionary<string, object> EngineCommandPayload,
    string CorrelationId,
    string RequestedBy
)
{
    public static WorkflowDispatchCommand Create(
        Guid workflowInstanceId,
        string stepId,
        string engineName,
        IReadOnlyDictionary<string, object> payload,
        string correlationId,
        string requestedBy)
        => new(workflowInstanceId, stepId, engineName, payload, correlationId, requestedBy);
}