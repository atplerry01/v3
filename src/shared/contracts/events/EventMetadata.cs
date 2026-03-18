namespace Whycespace.Contracts.Events;

public sealed record EventMetadata(
    string Source,
    string CorrelationId,
    string? CausationId = null,
    IReadOnlyDictionary<string, string>? Headers = null
);
