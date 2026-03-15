namespace Whycespace.Engines.T1M.WSS.Versioning;

using Whycespace.Contracts.Workflows;

public sealed record WorkflowVersionCommand(
    Guid CommandId,
    string WorkflowName,
    string BaseVersion,
    IReadOnlyList<WorkflowStep> NewDefinition,
    string ChangeDescription,
    string RequestedBy,
    DateTimeOffset Timestamp);
