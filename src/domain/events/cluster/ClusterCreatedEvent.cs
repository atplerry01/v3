namespace Whycespace.Domain.Events.Core.Cluster;

public sealed record ClusterCreatedEvent(
    Guid ClusterId,
    Guid WorkflowId,
    string PartitionKey,
    DateTimeOffset Timestamp
);
