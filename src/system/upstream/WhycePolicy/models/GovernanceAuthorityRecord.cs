namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record GovernanceAuthorityRecord(
    string ActorId,
    GovernanceRole Role,
    DateTime AssignedAt
);
