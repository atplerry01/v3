namespace Whycespace.Shared.Context;

public sealed record TenantContext(
    string TenantId,
    string TenantName,
    string Region
);
