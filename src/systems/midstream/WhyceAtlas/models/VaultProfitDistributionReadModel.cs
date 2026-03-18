namespace Whycespace.Systems.Midstream.WhyceAtlas.Projections.Models;

public sealed record VaultProfitDistributionReadModel(
    Guid DistributionId,
    Guid VaultId,
    Guid ParticipantId,
    decimal ProfitAmount,
    string Currency,
    string DistributionType,
    DateTime DistributionTimestamp,
    DateTime RecordedAt,
    string? DistributionReference = null,
    string? DistributionSummary = null
);
