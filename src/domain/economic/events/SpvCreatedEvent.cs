namespace Whycespace.Domain.Economic.Events;

public sealed record SpvCreatedEvent(
    Guid SpvId,
    Guid WorkflowId,
    string PartitionKey,
    DateTimeOffset Timestamp
);
