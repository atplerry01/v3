namespace Whycespace.Tests.Engines.Vault;

using Whycespace.Engines.T3I.Economic.Vault;
using Xunit;

public sealed class VaultEvidenceHashEngineTests
{
    private readonly VaultEvidenceHashEngine _engine = new();

    private static readonly Guid VaultId = Guid.NewGuid();
    private static readonly Guid RequestedBy = Guid.NewGuid();
    private static readonly DateTime EvidenceTimestamp = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private static HashVaultEvidenceCommand CreateCommand(
        Guid? hashId = null,
        Guid? evidenceId = null,
        Guid? vaultId = null,
        string? payload = null,
        Guid? requestedBy = null)
    {
        return new HashVaultEvidenceCommand(
            HashId: hashId ?? Guid.NewGuid(),
            EvidenceId: evidenceId ?? Guid.NewGuid(),
            VaultId: vaultId ?? VaultId,
            EvidencePayload: payload ?? "{\"vaultId\":\"abc\",\"amount\":100}",
            EvidenceTimestamp: EvidenceTimestamp,
            RequestedBy: requestedBy ?? RequestedBy);
    }

    [Fact]
    public void HashGenerationTest_SHA256HashGeneratedCorrectly()
    {
        var command = CreateCommand();

        var result = _engine.HashEvidence(command);

        Assert.Equal(command.HashId, result.HashId);
        Assert.Equal(command.EvidenceId, result.EvidenceId);
        Assert.Equal(command.VaultId, result.VaultId);
        Assert.NotEmpty(result.EvidenceHash);
        Assert.Equal(64, result.EvidenceHash.Length);
        Assert.Equal("SHA-256", result.HashAlgorithm);
    }

    [Fact]
    public void DeterministicHashTest_IdenticalPayloadsProduceIdenticalHashes()
    {
        var hashId1 = Guid.NewGuid();
        var hashId2 = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        const string payload = "{\"vaultId\":\"abc\",\"amount\":100}";

        var result1 = _engine.HashEvidence(CreateCommand(hashId: hashId1, evidenceId: evidenceId, payload: payload));
        var result2 = _engine.HashEvidence(CreateCommand(hashId: hashId2, evidenceId: evidenceId, payload: payload));

        Assert.Equal(result1.EvidenceHash, result2.EvidenceHash);
    }

    [Fact]
    public void HashIntegrityTest_ModifyingPayloadChangesHash()
    {
        var evidenceId = Guid.NewGuid();

        var result1 = _engine.HashEvidence(CreateCommand(evidenceId: evidenceId, payload: "{\"amount\":100}"));
        var result2 = _engine.HashEvidence(CreateCommand(evidenceId: evidenceId, payload: "{\"amount\":200}"));

        Assert.NotEqual(result1.EvidenceHash, result2.EvidenceHash);
    }

    [Fact]
    public void AlgorithmVerificationTest_SHA256UsedCorrectly()
    {
        var command = CreateCommand();

        var result = _engine.HashEvidence(command);

        Assert.Equal("SHA-256", result.HashAlgorithm);
        Assert.Equal(64, result.EvidenceHash.Length);
        Assert.True(result.EvidenceHash.All(c => "0123456789abcdef".Contains(c)));
    }

    [Fact]
    public void HashMetadataTest_MetadataFieldsPopulatedCorrectly()
    {
        var hashId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var command = CreateCommand(hashId: hashId, evidenceId: evidenceId);

        var result = _engine.HashEvidence(command);

        Assert.Equal(hashId, result.HashId);
        Assert.Equal(evidenceId, result.EvidenceId);
        Assert.Equal(VaultId, result.VaultId);
        Assert.NotEmpty(result.HashSummary);
        Assert.Contains(evidenceId.ToString(), result.HashSummary);
        Assert.Contains(VaultId.ToString(), result.HashSummary);
    }

    [Fact]
    public void EmptyHashId_Fails()
    {
        var command = CreateCommand(hashId: Guid.Empty);

        var result = _engine.HashEvidence(command);

        Assert.Empty(result.EvidenceHash);
        Assert.Contains("HashId", result.HashSummary);
    }

    [Fact]
    public void EmptyEvidenceId_Fails()
    {
        var command = CreateCommand(evidenceId: Guid.Empty);

        var result = _engine.HashEvidence(command);

        Assert.Empty(result.EvidenceHash);
        Assert.Contains("EvidenceId", result.HashSummary);
    }

    [Fact]
    public void EmptyVaultId_Fails()
    {
        var command = CreateCommand(vaultId: Guid.Empty);

        var result = _engine.HashEvidence(command);

        Assert.Empty(result.EvidenceHash);
        Assert.Contains("VaultId", result.HashSummary);
    }

    [Fact]
    public void EmptyPayload_Fails()
    {
        var command = CreateCommand(payload: "");

        var result = _engine.HashEvidence(command);

        Assert.Empty(result.EvidenceHash);
        Assert.Contains("EvidencePayload", result.HashSummary);
    }

    [Fact]
    public void EmptyRequestedBy_Fails()
    {
        var command = CreateCommand(requestedBy: Guid.Empty);

        var result = _engine.HashEvidence(command);

        Assert.Empty(result.EvidenceHash);
        Assert.Contains("RequestedBy", result.HashSummary);
    }

    [Fact]
    public void WhitespaceNormalization_LeadingTrailingTrimmed()
    {
        var result1 = _engine.HashEvidence(CreateCommand(payload: "  {\"a\":1}  "));
        var result2 = _engine.HashEvidence(CreateCommand(payload: "{\"a\":1}"));

        Assert.Equal(result1.EvidenceHash, result2.EvidenceHash);
    }
}
