namespace Whycespace.Engines.T3I.Reporting.Economic;

public sealed record VaultEvidenceHashResult(
    Guid HashId,
    Guid EvidenceId,
    Guid VaultId,
    string EvidenceHash,
    string HashAlgorithm,
    DateTime HashedAt,
    string HashSummary = "");
