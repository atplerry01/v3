namespace Whycespace.Systems.Downstream.Clusters.Providers;

public sealed record ClusterProviderRecord(
    Guid ProviderId,
    Guid IdentityId,
    string ClusterId,
    string SubClusterId,
    ClusterProviderType ProviderType,
    string Status,
    DateTimeOffset RegisteredAt,
    string? LicenseReference = null
);
