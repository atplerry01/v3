namespace Whycespace.Tests.Engines.Vault;

using Whycespace.Engines.T0U.WhyceChain.Block.Builder;
using Whycespace.Engines.T0U.WhyceChain.Block.Anchor;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Event;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Immutable;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Indexing;
using Whycespace.Engines.T0U.WhyceChain.Verification.Integrity;
using Whycespace.Engines.T0U.WhyceChain.Verification.Merkle;
using Whycespace.Engines.T0U.WhyceChain.Verification.Audit;
using Whycespace.Engines.T0U.WhyceChain.Replication.Replication;
using Whycespace.Engines.T0U.WhyceChain.Replication.Snapshot;
using Whycespace.Engines.T0U.WhyceChain.Append.Execution;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Hashing;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Anchoring;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Gateway;
using Whycespace.Engines.T2E.Economic.Vault.Adapters;
using Whycespace.Systems.Upstream.WhyceChain.Stores;
using Xunit;

public sealed class VaultEvidenceAnchorAdapterTests
{
    private readonly VaultEvidenceAnchorAdapter _adapter;
    private readonly ChainEvidenceGateway _gateway;

    private static readonly Guid VaultId = Guid.NewGuid();
    private static readonly Guid RequestedBy = Guid.NewGuid();
    private static readonly DateTime EvidenceTimestamp = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
    private const string SampleHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2";

    public VaultEvidenceAnchorAdapterTests()
    {
        var ledgerStore = new ChainLedgerStore();
        var eventStore = new ChainEventStore();
        var ledgerEngine = new ChainLedgerEngine(ledgerStore);
        var hashEngine = new EvidenceHashEngine();
        var eventLedgerEngine = new ImmutableEventLedgerEngine(eventStore);
        var anchoringEngine = new EvidenceAnchoringEngine(ledgerEngine, hashEngine, eventLedgerEngine);
        _gateway = new ChainEvidenceGateway(anchoringEngine, hashEngine);
        _adapter = new VaultEvidenceAnchorAdapter(_gateway);
    }

    private static AnchorVaultEvidenceCommand CreateCommand(
        Guid? anchorRequestId = null,
        Guid? evidenceId = null,
        Guid? vaultId = null,
        string? evidenceHash = null,
        DateTime? evidenceTimestamp = null,
        Guid? requestedBy = null,
        string? referenceId = null,
        string? referenceType = null)
    {
        return new AnchorVaultEvidenceCommand(
            AnchorRequestId: anchorRequestId ?? Guid.NewGuid(),
            EvidenceId: evidenceId ?? Guid.NewGuid(),
            VaultId: vaultId ?? VaultId,
            EvidenceHash: evidenceHash ?? SampleHash,
            EvidenceTimestamp: evidenceTimestamp ?? EvidenceTimestamp,
            RequestedBy: requestedBy ?? RequestedBy,
            ReferenceId: referenceId,
            ReferenceType: referenceType);
    }

    [Fact]
    public void AnchorPayloadConstructionTest_PayloadConstructedCorrectly()
    {
        var evidenceId = Guid.NewGuid();
        var command = CreateCommand(evidenceId: evidenceId);

        var result = _adapter.AnchorEvidence(command);

        Assert.Equal(command.AnchorRequestId, result.AnchorRequestId);
        Assert.Equal(command.EvidenceId, result.EvidenceId);
        Assert.Equal(command.VaultId, result.VaultId);
        Assert.Equal(command.EvidenceHash, result.EvidenceHash);
        Assert.NotEmpty(result.ChainTransactionId);
    }

    [Fact]
    public void AnchorSubmissionTest_AnchorSubmissionExecuted()
    {
        var command = CreateCommand();

        var result = _adapter.AnchorEvidence(command);

        Assert.Equal("Anchored", result.AnchorStatus);
        Assert.NotEmpty(result.ChainTransactionId);
        Assert.Equal(command.EvidenceId.ToString(), result.ChainTransactionId);
    }

    [Fact]
    public void AnchorConfirmationTest_ConfirmationReceivedAndRecorded()
    {
        var command = CreateCommand();

        var result = _adapter.AnchorEvidence(command);

        Assert.Equal("Anchored", result.AnchorStatus);
        Assert.True(result.AnchoredAt <= DateTime.UtcNow);
        Assert.True(result.AnchoredAt > DateTime.UtcNow.AddMinutes(-1));

        var proof = _gateway.GetEvidence(command.EvidenceId.ToString());
        Assert.NotNull(proof);
        Assert.NotEmpty(proof.Hash);
    }

    [Fact]
    public void AnchorStatusTest_AnchorStatusValuesHandledCorrectly()
    {
        var command = CreateCommand();

        var result = _adapter.AnchorEvidence(command);

        Assert.Equal("Anchored", result.AnchorStatus);
    }

    [Fact]
    public void DeterministicPayloadTest_IdenticalInputsProduceIdenticalAnchorPayloads()
    {
        var anchorRequestId1 = Guid.NewGuid();
        var anchorRequestId2 = Guid.NewGuid();
        var evidenceId1 = Guid.NewGuid();
        var evidenceId2 = Guid.NewGuid();
        const string hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        var result1 = _adapter.AnchorEvidence(CreateCommand(
            anchorRequestId: anchorRequestId1,
            evidenceId: evidenceId1,
            evidenceHash: hash));

        var result2 = _adapter.AnchorEvidence(CreateCommand(
            anchorRequestId: anchorRequestId2,
            evidenceId: evidenceId2,
            evidenceHash: hash));

        Assert.Equal(result1.EvidenceHash, result2.EvidenceHash);
        Assert.Equal(result1.VaultId, result2.VaultId);
    }

    [Fact]
    public void EmptyAnchorRequestId_Throws()
    {
        var command = CreateCommand(anchorRequestId: Guid.Empty);

        var ex = Assert.Throws<ArgumentException>(() => _adapter.AnchorEvidence(command));
        Assert.Contains("AnchorRequestId", ex.Message);
    }

    [Fact]
    public void EmptyEvidenceId_Throws()
    {
        var command = CreateCommand(evidenceId: Guid.Empty);

        var ex = Assert.Throws<ArgumentException>(() => _adapter.AnchorEvidence(command));
        Assert.Contains("EvidenceId", ex.Message);
    }

    [Fact]
    public void EmptyVaultId_Throws()
    {
        var command = CreateCommand(vaultId: Guid.Empty);

        var ex = Assert.Throws<ArgumentException>(() => _adapter.AnchorEvidence(command));
        Assert.Contains("VaultId", ex.Message);
    }

    [Fact]
    public void EmptyEvidenceHash_Throws()
    {
        var command = CreateCommand(evidenceHash: "");

        var ex = Assert.Throws<ArgumentException>(() => _adapter.AnchorEvidence(command));
        Assert.Contains("EvidenceHash", ex.Message);
    }

    [Fact]
    public void EmptyRequestedBy_Throws()
    {
        var command = CreateCommand(requestedBy: Guid.Empty);

        var ex = Assert.Throws<ArgumentException>(() => _adapter.AnchorEvidence(command));
        Assert.Contains("RequestedBy", ex.Message);
    }

    [Fact]
    public void OptionalMetadata_AcceptedCorrectly()
    {
        var command = CreateCommand(
            referenceId: "REF-001",
            referenceType: "VaultTransaction");

        var result = _adapter.AnchorEvidence(command);

        Assert.Equal("Anchored", result.AnchorStatus);
        Assert.Equal(command.EvidenceId, result.EvidenceId);
    }
}
