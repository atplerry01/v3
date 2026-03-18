namespace Whycespace.Contracts.Commands;

public sealed record CommandMetadata(
    string CommandType,
    string Source,
    string UserId,
    DateTimeOffset IssuedAt,
    IReadOnlyDictionary<string, string>? Headers = null
);
