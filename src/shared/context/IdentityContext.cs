namespace Whycespace.Shared.Context;

public sealed record IdentityContext(
    string UserId,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyDictionary<string, string> Claims
);
