namespace Whycespace.Domain.Clusters.Governance.Authority;

public sealed record ClusterCreatedEvent(
    Guid ClusterId,
    Guid WorkflowId,
    string PartitionKey,
    DateTimeOffset Timestamp
);
