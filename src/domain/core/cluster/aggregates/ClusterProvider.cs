namespace Whycespace.Domain.Core.Cluster.Aggregates;

public sealed record ClusterProvider(
    Guid ProviderId,
    string Name,
    string Platform,
    string Region,
    ClusterProviderStatus Status,
    DateTimeOffset OnboardedAt
);

public enum ClusterProviderStatus
{
    Active,
    Degraded,
    Offline
}
