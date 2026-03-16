namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyAuditQuery(
    string? PolicyId,
    string? ActorId,
    string? Domain,
    DateTime? From,
    DateTime? To
);
