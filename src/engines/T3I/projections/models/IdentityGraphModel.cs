namespace Whycespace.Engines.T3I.Projections.Models;

public sealed record IdentityGraphModel(
    Guid IdentityId,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Roles,
    string Status,
    DateTimeOffset LastUpdatedAt);
