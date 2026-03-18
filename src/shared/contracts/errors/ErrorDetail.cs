namespace Whycespace.Contracts.Errors;

public sealed record ErrorDetail(
    ErrorCode Code,
    string Message,
    string? Target = null,
    IReadOnlyDictionary<string, object>? Properties = null
);
