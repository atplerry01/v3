namespace Whycespace.Systems.Downstream.Cwg.Contributions;

public sealed record ContributionRecord(
    Guid ContributionId,
    Guid ParticipantId,
    Guid VaultId,
    string ContributionType,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt,
    string? Description = null
);
