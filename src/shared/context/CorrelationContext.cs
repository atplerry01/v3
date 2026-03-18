using Whycespace.Shared.Primitives.Common;

namespace Whycespace.Shared.Context;

public sealed record CorrelationContext(
    CorrelationId CorrelationId,
    CorrelationId? ParentCorrelationId = null,
    string? CausationId = null
);
