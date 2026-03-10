namespace Whycespace.Shared.Identity;

public sealed record WhyceIdentity(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyDictionary<string, string> Claims
);
