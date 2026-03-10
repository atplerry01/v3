namespace Whycespace.Observability.Models;

public sealed record DiagnosticEntry(
    Guid WorkflowId,
    string WorkflowName,
    string StepName,
    string PartitionKey,
    string Status,
    DateTime Timestamp
);
