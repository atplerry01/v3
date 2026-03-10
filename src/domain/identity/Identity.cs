namespace Whycespace.Domain.Identity;

public sealed record Identity(
    Guid IdentityId,
    string DisplayName,
    string Email,
    IReadOnlyList<Role> Roles,
    IdentityStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastAuthenticatedAt
);

public enum IdentityStatus
{
    Active,
    Suspended,
    Deactivated
}
