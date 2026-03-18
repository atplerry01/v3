using Whycespace.Shared.Primitives.Common;

namespace Whycespace.Shared.Context;

public sealed record RuntimeContext(
    CorrelationId CorrelationId,
    string TenantId,
    string UserId,
    DateTimeOffset Timestamp
)
{
    public static RuntimeContext Create(string tenantId, string userId)
        => new(CorrelationId.New(), tenantId, userId, DateTimeOffset.UtcNow);
}
