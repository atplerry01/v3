namespace Whycespace.Tests.Engines.Vault;

using Whycespace.Engines.T3I.Economic.Vault;
using Xunit;

public sealed class VaultEvidenceRecorderTests
{
    private readonly VaultEvidenceRecorder _recorder = new();

    private static readonly Guid VaultId = Guid.NewGuid();
    private static readonly Guid TransactionId = Guid.NewGuid();
    private static readonly Guid RequestedBy = Guid.NewGuid();
    private static readonly DateTime Timestamp = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

    private static RecordVaultEvidenceCommand CreateCommand(
        string evidenceType = "VaultContributionRecorded",
        Guid? evidenceId = null,
        Guid? vaultId = null,
        Guid? transactionId = null,
        Guid? requestedBy = null,
        DateTime? timestamp = null,
        string referenceId = "",
        string referenceType = "")
    {
        return new RecordVaultEvidenceCommand(
            EvidenceId: evidenceId ?? Guid.NewGuid(),
            VaultId: vaultId ?? VaultId,
            TransactionId: transactionId ?? TransactionId,
            EvidenceType: evidenceType,
            EvidenceTimestamp: timestamp ?? Timestamp,
            RequestedBy: requestedBy ?? RequestedBy,
            ReferenceId: referenceId,
            ReferenceType: referenceType);
    }

    [Fact]
    public void EvidenceRecordCreationTest_RecordGeneratedSuccessfully()
    {
        var command = CreateCommand();

        var result = _recorder.RecordEvidence(command);

        Assert.Equal(command.EvidenceId, result.EvidenceId);
        Assert.Equal(command.VaultId, result.VaultId);
        Assert.Equal(command.TransactionId, result.TransactionId);
        Assert.Equal(command.EvidenceType, result.EvidenceType);
        Assert.NotEmpty(result.EvidencePayload);
        Assert.NotEmpty(result.EvidenceHashCandidate);
        Assert.Contains("Evidence recorded", result.EvidenceSummary);
    }

    [Fact]
    public void PayloadSerializationTest_PayloadSerializedDeterministically()
    {
        var command = CreateCommand();

        var result1 = _recorder.RecordEvidence(command);
        var result2 = _recorder.RecordEvidence(command);

        Assert.Equal(result1.EvidencePayload, result2.EvidencePayload);
    }

    [Fact]
    public void HashCandidatePreparationTest_HashCandidatePreparedCorrectly()
    {
        var command = CreateCommand();

        var result = _recorder.RecordEvidence(command);

        Assert.NotEmpty(result.EvidenceHashCandidate);
        Assert.Equal(64, result.EvidenceHashCandidate.Length);
        Assert.Matches("^[0-9a-f]{64}$", result.EvidenceHashCandidate);
    }

    [Fact]
    public void EvidenceMetadataTest_MetadataFieldsPopulatedCorrectly()
    {
        var command = CreateCommand(
            referenceId: "REF-001",
            referenceType: "ExternalAudit");

        var result = _recorder.RecordEvidence(command);

        Assert.Contains("REF-001", result.EvidencePayload);
        Assert.Contains("ExternalAudit", result.EvidencePayload);
        Assert.Equal(command.EvidenceTimestamp, result.EvidenceTimestamp);
    }

    [Fact]
    public void DeterministicEvidenceTest_IdenticalInputsProduceIdenticalRecords()
    {
        var evidenceId = Guid.NewGuid();
        var command = CreateCommand(evidenceId: evidenceId);

        var result1 = _recorder.RecordEvidence(command);
        var result2 = _recorder.RecordEvidence(command);

        Assert.Equal(result1.EvidenceId, result2.EvidenceId);
        Assert.Equal(result1.EvidencePayload, result2.EvidencePayload);
        Assert.Equal(result1.EvidenceHashCandidate, result2.EvidenceHashCandidate);
        Assert.Equal(result1.EvidenceType, result2.EvidenceType);
    }

    [Fact]
    public void EmptyEvidenceId_Fails()
    {
        var command = CreateCommand(evidenceId: Guid.Empty);

        var result = _recorder.RecordEvidence(command);

        Assert.Contains("EvidenceId", result.EvidenceSummary);
        Assert.Empty(result.EvidencePayload);
        Assert.Empty(result.EvidenceHashCandidate);
    }

    [Fact]
    public void EmptyVaultId_Fails()
    {
        var command = CreateCommand(vaultId: Guid.Empty);

        var result = _recorder.RecordEvidence(command);

        Assert.Contains("VaultId", result.EvidenceSummary);
        Assert.Empty(result.EvidencePayload);
    }

    [Fact]
    public void EmptyTransactionId_Fails()
    {
        var command = CreateCommand(transactionId: Guid.Empty);

        var result = _recorder.RecordEvidence(command);

        Assert.Contains("TransactionId", result.EvidenceSummary);
    }

    [Fact]
    public void EmptyRequestedBy_Fails()
    {
        var command = CreateCommand(requestedBy: Guid.Empty);

        var result = _recorder.RecordEvidence(command);

        Assert.Contains("RequestedBy", result.EvidenceSummary);
    }

    [Fact]
    public void InvalidEvidenceType_Fails()
    {
        var command = CreateCommand(evidenceType: "InvalidType");

        var result = _recorder.RecordEvidence(command);

        Assert.Contains("Invalid evidence type", result.EvidenceSummary);
        Assert.Empty(result.EvidencePayload);
    }

    [Theory]
    [InlineData("VaultCreated")]
    [InlineData("VaultTransactionExecuted")]
    [InlineData("VaultContributionRecorded")]
    [InlineData("VaultTransferExecuted")]
    [InlineData("VaultWithdrawalExecuted")]
    [InlineData("VaultProfitDistributed")]
    [InlineData("VaultSnapshotCreated")]
    public void AllValidEvidenceTypes_Succeed(string evidenceType)
    {
        var command = CreateCommand(evidenceType: evidenceType);

        var result = _recorder.RecordEvidence(command);

        Assert.Equal(evidenceType, result.EvidenceType);
        Assert.NotEmpty(result.EvidencePayload);
        Assert.NotEmpty(result.EvidenceHashCandidate);
    }

    [Fact]
    public void PayloadContainsAllRequiredFields()
    {
        var command = CreateCommand();

        var result = _recorder.RecordEvidence(command);

        Assert.Contains("vaultId", result.EvidencePayload);
        Assert.Contains("transactionId", result.EvidencePayload);
        Assert.Contains("evidenceType", result.EvidencePayload);
        Assert.Contains("evidenceTimestamp", result.EvidencePayload);
        Assert.Contains("requestedBy", result.EvidencePayload);
        Assert.Contains("evidenceId", result.EvidencePayload);
    }
}
