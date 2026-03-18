namespace Whycespace.Contracts.Evidence;

public sealed record EvidenceMetadata(
    string RecorderId,
    string OperationType,
    DateTimeOffset RecordedAt,
    string HashAlgorithm,
    string? CorrelationId = null
);
