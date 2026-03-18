namespace Whycespace.Domain.Clusters.Governance.Authority;

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
