namespace Whycespace.Shared.Primitives.Identity;

public sealed record WhyceIdentity(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyDictionary<string, string> Claims
);
