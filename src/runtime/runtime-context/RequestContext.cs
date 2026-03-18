namespace Whycespace.Runtime.Context;

public sealed record RequestContext(
    string RequestId,
    DateTimeOffset ReceivedAt,
    string? SourceSystem = null,
    string? OperatorId = null
);
