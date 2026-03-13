namespace Whycespace.Domain.Events.Cluster;

public sealed record ClusterCreatedEvent(
    Guid ClusterId,
    Guid WorkflowId,
    string PartitionKey,
    DateTimeOffset Timestamp
);
