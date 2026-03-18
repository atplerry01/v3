namespace Whycespace.Domain.Events.Spv;

public sealed record SpvCreatedEvent(
    Guid SpvId,
    Guid WorkflowId,
    string PartitionKey,
    DateTimeOffset Timestamp
);
