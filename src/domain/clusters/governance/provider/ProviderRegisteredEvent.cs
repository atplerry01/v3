namespace Whycespace.Domain.Clusters.Governance.Provider;

public sealed record ProviderRegisteredEvent(
    Guid ProviderId,
    Guid WorkflowId,
    string PartitionKey,
    string ProviderName,
    string ProviderType,
    DateTimeOffset Timestamp
);
