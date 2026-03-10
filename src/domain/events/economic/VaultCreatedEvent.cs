namespace Whycespace.Domain.Events.Economic;

public sealed record VaultCreatedEvent(
    Guid VaultId,
    Guid WorkflowId,
    string PartitionKey,
    string OwnerId,
    decimal Balance,
    string Currency,
    DateTimeOffset Timestamp
);
