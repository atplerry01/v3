using Whycespace.Shared.Primitives.Common;

namespace Whycespace.Shared.Context;

public sealed record RequestContext(
    CorrelationId CorrelationId,
    IdentityContext Identity,
    TenantContext Tenant,
    DateTimeOffset RequestedAt
);
