namespace Whycespace.Systems.Downstream.Spv.Registry;

public sealed record SpvRegistryRecord(
    Guid SpvId,
    string Name,
    string ClusterId,
    decimal AllocatedCapital,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ActivatedAt = null,
    string? GovernancePolicy = null
);
