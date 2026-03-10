namespace Whycespace.Domain.Events.Providers;

public sealed record ProviderRegisteredEvent(
    Guid ProviderId,
    Guid WorkflowId,
    string PartitionKey,
    string ProviderName,
    string ProviderType,
    DateTimeOffset Timestamp
);
