using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.WhyceChain.Ledger;

namespace Whycespace.WhyceChain.Evidence.Tests;

public class EvidenceHashEngineTests
{
    private readonly EvidenceHashEngine _engine;

    public EvidenceHashEngineTests()
    {
        _engine = new EvidenceHashEngine();
    }

    private static EvidenceHashCommand CreateCommand(
        string evidenceType = "PolicyDecision",
        object? payload = null,
        string traceId = "trace-001",
        string correlationId = "corr-001",
        DateTime? timestamp = null)
    {
        return new EvidenceHashCommand(
            EvidenceType: evidenceType,
            EvidencePayload: payload ?? new { PolicyId = "pol-1", Decision = "allow" },
            TraceId: traceId,
            CorrelationId: correlationId,
            Timestamp: timestamp ?? new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Execute_ShouldReturnDeterministicHash()
    {
        var command = CreateCommand();

        var first = _engine.Execute(command);
        var second = _engine.Execute(command);

        Assert.Equal(first.EvidenceHash, second.EvidenceHash);
        Assert.Equal(first.PayloadCanonicalHash, second.PayloadCanonicalHash);
        Assert.Equal(first.MetadataHash, second.MetadataHash);
        Assert.Equal("SHA256", first.HashAlgorithm);
    }

    [Fact]
    public void Execute_IdenticalPayload_ProducesIdenticalHash()
    {
        var command1 = CreateCommand(payload: new { Name = "test", Value = 42 });
        var command2 = CreateCommand(payload: new { Name = "test", Value = 42 });

        var result1 = _engine.Execute(command1);
        var result2 = _engine.Execute(command2);

        Assert.Equal(result1.PayloadCanonicalHash, result2.PayloadCanonicalHash);
        Assert.Equal(result1.EvidenceHash, result2.EvidenceHash);
    }

    [Fact]
    public void Execute_PayloadMutation_ChangesHash()
    {
        var original = CreateCommand(payload: new { Name = "test", Value = 42 });
        var mutated = CreateCommand(payload: new { Name = "test", Value = 43 });

        var result1 = _engine.Execute(original);
        var result2 = _engine.Execute(mutated);

        Assert.NotEqual(result1.PayloadCanonicalHash, result2.PayloadCanonicalHash);
        Assert.NotEqual(result1.EvidenceHash, result2.EvidenceHash);
    }

    [Fact]
    public void Execute_MetadataVariation_AffectsFinalHash()
    {
        var command1 = CreateCommand(evidenceType: "PolicyDecision");
        var command2 = CreateCommand(evidenceType: "IdentityVerification");

        var result1 = _engine.Execute(command1);
        var result2 = _engine.Execute(command2);

        Assert.Equal(result1.PayloadCanonicalHash, result2.PayloadCanonicalHash);
        Assert.NotEqual(result1.MetadataHash, result2.MetadataHash);
        Assert.NotEqual(result1.EvidenceHash, result2.EvidenceHash);
    }

    [Fact]
    public void Execute_ShouldReturnCorrectTraceId()
    {
        var command = CreateCommand(traceId: "trace-xyz");
        var result = _engine.Execute(command);

        Assert.Equal("trace-xyz", result.TraceId);
    }

    [Fact]
    public void Execute_ShouldReturnCommandTimestamp()
    {
        var ts = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var command = CreateCommand(timestamp: ts);
        var result = _engine.Execute(command);

        Assert.Equal(ts, result.GeneratedAt);
    }

    [Fact]
    public void Execute_ConcurrencyHashingTest()
    {
        var command = CreateCommand();
        var results = new EvidenceHashResult[100];

        Parallel.For(0, 100, i =>
        {
            results[i] = _engine.Execute(command);
        });

        var expected = results[0].EvidenceHash;
        Assert.All(results, r => Assert.Equal(expected, r.EvidenceHash));
    }

    [Fact]
    public void Execute_AllEvidenceTypes_ProduceDeterministicHashes()
    {
        var evidenceTypes = new[]
        {
            "PolicyDecision",
            "IdentityVerification",
            "WorkflowOutcome",
            "LedgerEntry",
            "ChainBlock",
            "GovernanceAction"
        };

        foreach (var evidenceType in evidenceTypes)
        {
            var command = CreateCommand(evidenceType: evidenceType);
            var first = _engine.Execute(command);
            var second = _engine.Execute(command);

            Assert.Equal(first.EvidenceHash, second.EvidenceHash);
        }
    }

    [Fact]
    public void Execute_PropertyOrderDoesNotAffectPayloadHash()
    {
        var command1 = CreateCommand(payload: new { A = 1, B = 2 });
        var command2 = CreateCommand(payload: new { B = 2, A = 1 });

        var result1 = _engine.Execute(command1);
        var result2 = _engine.Execute(command2);

        Assert.Equal(result1.PayloadCanonicalHash, result2.PayloadCanonicalHash);
    }

    [Fact]
    public void Execute_HashComponentsAreDistinct()
    {
        var result = _engine.Execute(CreateCommand());

        Assert.NotEqual(result.PayloadCanonicalHash, result.MetadataHash);
        Assert.NotEqual(result.PayloadCanonicalHash, result.EvidenceHash);
        Assert.NotEqual(result.MetadataHash, result.EvidenceHash);
    }
}
