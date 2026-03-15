using Whycespace.Engines.T0U.Governance;
using Whycespace.Engines.T0U.Governance.Commands;
using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.Governance.Evidence.Models;
using Whycespace.System.Upstream.WhyceChain.Stores;

namespace Whycespace.Governance.Tests;

public class GovernanceEvidenceRecorderExecuteTests
{
    private readonly GovernanceEvidenceRecorder _recorder;

    public GovernanceEvidenceRecorderExecuteTests()
    {
        var ledgerStore = new ChainLedgerStore();
        var eventStore = new ChainEventStore();
        var ledgerEngine = new ChainLedgerEngine(ledgerStore);
        var hashEngine = new EvidenceHashEngine();
        var eventLedgerEngine = new ImmutableEventLedgerEngine(eventStore);
        var anchoringEngine = new EvidenceAnchoringEngine(ledgerEngine, hashEngine, eventLedgerEngine);
        var gateway = new ChainEvidenceGateway(anchoringEngine, hashEngine);
        _recorder = new GovernanceEvidenceRecorder(gateway);
    }

    [Fact]
    public void Execute_ValidCommand_RecordsEvidence()
    {
        var command = CreateValidCommand();

        var result = _recorder.Execute(command);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.EvidenceId);
        Assert.Equal(command.ProposalId, result.ProposalId);
        Assert.Equal(command.EvidenceType, result.EvidenceType);
        Assert.Equal("Governance evidence recorded successfully.", result.Message);
    }

    [Fact]
    public void Execute_ValidCommand_GeneratesEvidenceHash()
    {
        var command = CreateValidCommand();

        var result = _recorder.Execute(command);

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.EvidenceHash));
    }

    [Fact]
    public void Execute_ValidCommand_GeneratesMerkleRoot()
    {
        var command = CreateValidCommand();

        var result = _recorder.Execute(command);

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.MerkleRoot));
    }

    [Fact]
    public void Execute_DeterministicHash_SameInputProducesSameHash()
    {
        var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var proposalId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var eventRefId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var guardianId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var command1 = new RecordGovernanceEvidenceCommand(
            Guid.NewGuid(), proposalId, eventRefId,
            EvidenceType.ProposalCreated, guardianId,
            "{\"action\":\"create\"}", timestamp);

        var command2 = new RecordGovernanceEvidenceCommand(
            Guid.NewGuid(), proposalId, eventRefId,
            EvidenceType.ProposalCreated, guardianId,
            "{\"action\":\"create\"}", timestamp);

        var result1 = _recorder.Execute(command1);
        var result2 = _recorder.Execute(command2);

        Assert.Equal(result1.EvidenceHash, result2.EvidenceHash);
        Assert.Equal(result1.MerkleRoot, result2.MerkleRoot);
    }

    [Fact]
    public void Execute_EmptyProposalId_ReturnsFailure()
    {
        var command = CreateValidCommand() with { ProposalId = Guid.Empty };

        var result = _recorder.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Proposal ID", result.Message);
    }

    [Fact]
    public void Execute_EmptyEventReferenceId_ReturnsFailure()
    {
        var command = CreateValidCommand() with { EventReferenceId = Guid.Empty };

        var result = _recorder.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Event reference ID", result.Message);
    }

    [Fact]
    public void Execute_EmptyPayload_ReturnsFailure()
    {
        var command = CreateValidCommand() with { EvidencePayload = "" };

        var result = _recorder.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("payload", result.Message);
    }

    [Fact]
    public void Execute_WhitespacePayload_ReturnsFailure()
    {
        var command = CreateValidCommand() with { EvidencePayload = "   " };

        var result = _recorder.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("payload", result.Message);
    }

    [Fact]
    public void Execute_EmptyGuardianId_ReturnsFailure()
    {
        var command = CreateValidCommand() with { RecordedByGuardianId = Guid.Empty };

        var result = _recorder.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("guardian ID", result.Message);
    }

    [Fact]
    public void Execute_AllEvidenceTypes_Succeed()
    {
        foreach (var evidenceType in Enum.GetValues<EvidenceType>())
        {
            var command = CreateValidCommand() with { EvidenceType = evidenceType };
            var result = _recorder.Execute(command);

            Assert.True(result.Success, $"Failed for evidence type: {evidenceType}");
            Assert.Equal(evidenceType, result.EvidenceType);
        }
    }

    [Fact]
    public void Execute_ConcurrentRecordings_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => _recorder.Execute(CreateValidCommand())))
            .ToArray();

        Task.WaitAll(tasks);

        foreach (var task in tasks)
        {
            Assert.True(task.Result.Success);
            Assert.NotEqual(Guid.Empty, task.Result.EvidenceId);
        }

        var evidenceIds = tasks.Select(t => t.Result.EvidenceId).ToHashSet();
        Assert.Equal(10, evidenceIds.Count);
    }

    [Fact]
    public void Execute_DifferentPayloads_ProduceDifferentHashes()
    {
        var command1 = CreateValidCommand() with { EvidencePayload = "{\"action\":\"create\"}" };
        var command2 = CreateValidCommand() with { EvidencePayload = "{\"action\":\"update\"}" };

        var result1 = _recorder.Execute(command1);
        var result2 = _recorder.Execute(command2);

        Assert.NotEqual(result1.EvidenceHash, result2.EvidenceHash);
    }

    private static RecordGovernanceEvidenceCommand CreateValidCommand()
    {
        return new RecordGovernanceEvidenceCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: Guid.NewGuid(),
            EventReferenceId: Guid.NewGuid(),
            EvidenceType: EvidenceType.ProposalCreated,
            RecordedByGuardianId: Guid.NewGuid(),
            EvidencePayload: "{\"proposal\":\"test-proposal\",\"action\":\"create\"}",
            Timestamp: DateTime.UtcNow);
    }
}
