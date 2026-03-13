namespace Whycespace.Domain.Core.Economic;

public sealed record Asset(
    Guid AssetId,
    Guid SpvId,
    string AssetType,
    string Description,
    decimal Value,
    DateTimeOffset AcquiredAt
);
