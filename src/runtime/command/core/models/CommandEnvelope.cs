namespace Whycespace.CommandSystem.Core.Models;

public sealed record CommandEnvelope(
    Guid CommandId,
    string CommandType,
    IReadOnlyDictionary<string, object> Payload,
    DateTimeOffset Timestamp
);
