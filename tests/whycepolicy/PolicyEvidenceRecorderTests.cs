using Whycespace.Engines.T3I.Reporting.Policy;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyEvidenceRecorderTests
{
    private static readonly DateTime FixedTimestamp = new(2026, 3, 14, 12, 0, 0, DateTimeKind.Utc);

    private static PolicyEvidenceInput MakeInput(
        string policyId = "policy-001",
        string actionType = PolicyEvidenceActionType.POLICY_CREATED,
        string actorId = "actor-001",
        Dictionary<string, object>? evidenceContext = null,
        DateTime? timestamp = null) =>
        new(policyId, actionType, actorId,
            evidenceContext ?? new Dictionary<string, object> { ["domain"] = "platform", ["version"] = "1" },
            timestamp ?? FixedTimestamp);

    [Fact]
    public void RecordEvidence_ProducesValidRecord()
    {
        var engine = new PolicyEvidenceRecorder();
        var input = MakeInput();

        var result = engine.RecordEvidence(input);

        Assert.NotNull(result);
        Assert.Equal("policy-001", result.PolicyId);
        Assert.Equal(PolicyEvidenceActionType.POLICY_CREATED, result.ActionType);
        Assert.Equal("actor-001", result.ActorId);
        Assert.NotEmpty(result.EvidenceId);
        Assert.NotEmpty(result.EvidenceHash);
        Assert.NotEmpty(result.ContextHash);
        Assert.Equal(FixedTimestamp, result.RecordedAt);
    }

    [Fact]
    public void RecordEvidence_DeterministicEvidenceHash_SameInputsSameOutput()
    {
        var engine = new PolicyEvidenceRecorder();
        var input = MakeInput();

        var result1 = engine.RecordEvidence(input);
        var result2 = engine.RecordEvidence(input);

        Assert.Equal(result1.EvidenceHash, result2.EvidenceHash);
        Assert.Equal(result1.EvidenceId, result2.EvidenceId);
        Assert.Equal(result1.ContextHash, result2.ContextHash);
    }

    [Fact]
    public void GenerateContextHash_Consistent_ForSameContext()
    {
        var context = new Dictionary<string, object> { ["domain"] = "platform", ["version"] = "1" };

        var hash1 = PolicyEvidenceHashGenerator.GenerateContextHash(context);
        var hash2 = PolicyEvidenceHashGenerator.GenerateContextHash(context);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
    }

    [Fact]
    public void GenerateContextHash_DeterministicRegardlessOfInsertionOrder()
    {
        var context1 = new Dictionary<string, object> { ["a"] = "1", ["b"] = "2", ["c"] = "3" };
        var context2 = new Dictionary<string, object> { ["c"] = "3", ["a"] = "1", ["b"] = "2" };

        var hash1 = PolicyEvidenceHashGenerator.GenerateContextHash(context1);
        var hash2 = PolicyEvidenceHashGenerator.GenerateContextHash(context2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GenerateContextHash_DifferentContextsProduceDifferentHashes()
    {
        var context1 = new Dictionary<string, object> { ["domain"] = "platform" };
        var context2 = new Dictionary<string, object> { ["domain"] = "governance" };

        var hash1 = PolicyEvidenceHashGenerator.GenerateContextHash(context1);
        var hash2 = PolicyEvidenceHashGenerator.GenerateContextHash(context2);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void RecordEvidence_DifferentActionTypesProduceDifferentHashes()
    {
        var engine = new PolicyEvidenceRecorder();

        var created = engine.RecordEvidence(MakeInput(actionType: PolicyEvidenceActionType.POLICY_CREATED));
        var evaluated = engine.RecordEvidence(MakeInput(actionType: PolicyEvidenceActionType.POLICY_EVALUATED));

        Assert.NotEqual(created.EvidenceHash, evaluated.EvidenceHash);
        Assert.NotEqual(created.EvidenceId, evaluated.EvidenceId);
    }

    [Fact]
    public void RecordEvidence_ConcurrentRecording_ThreadSafe()
    {
        var engine = new PolicyEvidenceRecorder();
        var results = new global::System.Collections.Concurrent.ConcurrentBag<Whycespace.Engines.T3I.WhycePolicy.PolicyEvidenceRecord>();

        Parallel.For(0, 100, i =>
        {
            var input = MakeInput(
                policyId: $"policy-{i:D3}",
                actorId: $"actor-{i:D3}",
                timestamp: FixedTimestamp.AddMinutes(i));
            var record = engine.RecordEvidence(input);
            results.Add(record);
        });

        Assert.Equal(100, results.Count);
        var evidenceIds = results.Select(r => r.EvidenceId).ToHashSet();
        Assert.Equal(100, evidenceIds.Count);
    }

    [Fact]
    public void RecordEvidence_LargeContext_HandledCorrectly()
    {
        var engine = new PolicyEvidenceRecorder();
        var largeContext = new Dictionary<string, object>();
        for (var i = 0; i < 10000; i++)
            largeContext[$"key{i}"] = $"value{i}";

        var input = MakeInput(evidenceContext: largeContext);

        var result = engine.RecordEvidence(input);

        Assert.NotNull(result);
        Assert.NotEmpty(result.EvidenceHash);
        Assert.NotEmpty(result.ContextHash);
        Assert.Equal(64, result.ContextHash.Length);
        Assert.Equal(64, result.EvidenceHash.Length);
    }

    [Fact]
    public void RecordEvidence_ImmutableRecord_FieldsMatchInput()
    {
        var engine = new PolicyEvidenceRecorder();
        var input = MakeInput();

        var record = engine.RecordEvidence(input);

        Assert.Equal(input.PolicyId, record.PolicyId);
        Assert.Equal(input.ActionType, record.ActionType);
        Assert.Equal(input.ActorId, record.ActorId);
        Assert.Equal(input.Timestamp, record.RecordedAt);
    }
}
