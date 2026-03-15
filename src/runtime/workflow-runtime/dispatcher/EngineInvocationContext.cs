namespace Whycespace.WorkflowRuntime.Dispatcher;

public sealed record EngineInvocationContext(
    Guid WorkflowInstanceId,
    string WorkflowStepId,
    string EngineName,
    string EngineVersion,
    Guid InvocationId,
    string CorrelationId,
    string RequestedBy,
    DateTimeOffset Timestamp
)
{
    public static EngineInvocationContext Create(
        WorkflowDispatchCommand command,
        Guid invocationId,
        string engineVersion)
        => new(
            command.WorkflowInstanceId,
            command.StepId,
            command.EngineName,
            engineVersion,
            invocationId,
            command.CorrelationId,
            command.RequestedBy,
            DateTimeOffset.UtcNow);
}
