namespace Whycespace.Contracts.Errors;

public sealed record FailureReason(
    ErrorCode Code,
    string Description,
    string? Component = null,
    DateTimeOffset? OccurredAt = null
);
