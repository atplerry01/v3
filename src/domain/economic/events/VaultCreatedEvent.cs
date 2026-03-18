namespace Whycespace.Domain.Economic.Events;

public sealed record VaultCreatedEvent(
    Guid VaultId,
    Guid WorkflowId,
    string PartitionKey,
    string OwnerId,
    decimal Balance,
    string Currency,
    DateTimeOffset Timestamp
);
