namespace Whycespace.Domain.Economic;

public sealed record Revenue(
    Guid RevenueId,
    Guid SpvId,
    Guid AssetId,
    decimal Amount,
    string Source,
    DateTimeOffset RecordedAt
);
