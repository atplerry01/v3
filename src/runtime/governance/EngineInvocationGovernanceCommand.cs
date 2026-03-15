namespace Whycespace.RuntimeGovernance;

public sealed record EngineInvocationGovernanceCommand(
    Guid InvocationId,
    Guid WorkflowInstanceId,
    string WorkflowStepId,
    string EngineName,
    string EngineVersion,
    string RequestedBy,
    Guid CorrelationId,
    DateTime Timestamp);
