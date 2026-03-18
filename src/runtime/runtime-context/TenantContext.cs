namespace Whycespace.Runtime.Context;

public sealed record TenantContext(
    string TenantId,
    string? ClusterId = null,
    string? PartitionKey = null
);
