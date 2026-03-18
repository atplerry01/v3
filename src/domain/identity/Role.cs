namespace Whycespace.Domain.Core.Identity;

public sealed record Role(
    Guid RoleId,
    string Name,
    string Description,
    IReadOnlyList<Permission> Permissions,
    DateTimeOffset CreatedAt
);
