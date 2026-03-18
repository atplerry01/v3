using Whycespace.Engines.T3I.Reporting.Economic.Models;
using Whycespace.Engines.T3I.Shared;
namespace Whycespace.Engines.T3I.Reporting.Economic.Engines;

using global::System.Security.Cryptography;
using global::System.Text;

public sealed class VaultEvidenceHashEngine : IIntelligenceEngine<HashVaultEvidenceCommand, VaultEvidenceHashResult>
{
    private const string Algorithm = "SHA-256";

    public string EngineName => "VaultEvidenceHash";

    public IntelligenceResult<VaultEvidenceHashResult> Execute(IntelligenceContext<HashVaultEvidenceCommand> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var result = HashEvidence(context.Input);
        return IntelligenceResult<VaultEvidenceHashResult>.Ok(result, IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    public VaultEvidenceHashResult HashEvidence(HashVaultEvidenceCommand command)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return new VaultEvidenceHashResult(
                HashId: command.HashId,
                EvidenceId: command.EvidenceId,
                VaultId: command.VaultId,
                EvidenceHash: string.Empty,
                HashAlgorithm: Algorithm,
                HashedAt: DateTime.UtcNow,
                HashSummary: validationError);
        }

        var normalized = NormalizePayload(command.EvidencePayload);
        var hash = ComputeHash(normalized);

        return new VaultEvidenceHashResult(
            HashId: command.HashId,
            EvidenceId: command.EvidenceId,
            VaultId: command.VaultId,
            EvidenceHash: hash,
            HashAlgorithm: Algorithm,
            HashedAt: DateTime.UtcNow,
            HashSummary: $"Evidence {command.EvidenceId} hashed for vault {command.VaultId}");
    }

    private static string NormalizePayload(string payload)
    {
        return payload.Trim().ReplaceLineEndings("\n");
    }

    private static string ComputeHash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(bytes);
    }

    private static string? Validate(HashVaultEvidenceCommand command)
    {
        if (command.HashId == Guid.Empty)
            return "HashId must not be empty";

        if (command.EvidenceId == Guid.Empty)
            return "EvidenceId must not be empty";

        if (command.VaultId == Guid.Empty)
            return "VaultId must not be empty";

        if (string.IsNullOrWhiteSpace(command.EvidencePayload))
            return "EvidencePayload must not be empty";

        if (command.RequestedBy == Guid.Empty)
            return "RequestedBy must not be empty";

        return null;
    }
}
