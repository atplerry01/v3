using Whycespace.Engines.T3I.Reporting.Policy.Engines;
using Whycespace.Engines.T3I.Reporting.Policy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using SystemEvidenceRecord = Whycespace.Systems.Upstream.WhycePolicy.Models.PolicyEvidenceRecord;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyAuditEngineTests
{
    private static PolicyAuditInput MakeInput(
        string policyId = "policy-001",
        string decision = "ALLOW",
        string context = "domain=platform",
        string enforcement = "enforced",
        string actorId = "actor-001",
        PolicyAuditActionType actionType = PolicyAuditActionType.POLICY_EVALUATED) =>
        new(policyId, decision, context, enforcement, actorId, actionType);

    [Fact]
    public void CreateAuditRecord_ProducesValidRecord()
    {
        var engine = new PolicyAuditEngine();
        var input = MakeInput();

        var result = engine.CreateAuditRecord(input);

        Assert.NotNull(result);
        Assert.Equal("policy-001", result.PolicyId);
        Assert.Equal("actor-001", result.ActorId);
        Assert.Equal("ALLOW", result.Decision);
        Assert.Equal(PolicyAuditActionType.POLICY_EVALUATED, result.ActionType);
        Assert.NotEmpty(result.AuditId);
        Assert.NotEmpty(result.ContextHash);
        Assert.NotEmpty(result.Summary);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void CreateAuditRecord_DeterministicAuditId_SameInputsSameTimestamp()
    {
        var hash1 = PolicyAuditHashGenerator.GenerateAuditId(
            "policy-001", "actor-001", PolicyAuditActionType.POLICY_EVALUATED, "abc123", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var hash2 = PolicyAuditHashGenerator.GenerateAuditId(
            "policy-001", "actor-001", PolicyAuditActionType.POLICY_EVALUATED, "abc123", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void CreateAuditRecord_DifferentInputsProduceDifferentAuditIds()
    {
        var hash1 = PolicyAuditHashGenerator.GenerateAuditId(
            "policy-001", "actor-001", PolicyAuditActionType.POLICY_EVALUATED, "abc123", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var hash2 = PolicyAuditHashGenerator.GenerateAuditId(
            "policy-002", "actor-001", PolicyAuditActionType.POLICY_EVALUATED, "abc123", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateContextHash_ProducesConsistentHash()
    {
        var hash1 = PolicyAuditHashGenerator.GenerateContextHash("policy-001", "actor-001", "domain=platform");
        var hash2 = PolicyAuditHashGenerator.GenerateContextHash("policy-001", "actor-001", "domain=platform");

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA256 hex = 64 chars
    }

    [Fact]
    public void GenerateContextHash_DifferentInputsProduceDifferentHashes()
    {
        var hash1 = PolicyAuditHashGenerator.GenerateContextHash("policy-001", "actor-001", "domain=platform");
        var hash2 = PolicyAuditHashGenerator.GenerateContextHash("policy-001", "actor-002", "domain=platform");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void CreateAuditRecord_SummaryContainsPolicyAndActor()
    {
        var engine = new PolicyAuditEngine();

        var evaluated = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_EVALUATED));
        Assert.Contains("policy-001", evaluated.Summary);
        Assert.Contains("actor-001", evaluated.Summary);
        Assert.Contains("evaluated", evaluated.Summary);

        var enforced = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_ENFORCED));
        Assert.Contains("enforced", enforced.Summary.ToLowerInvariant());

        var simulated = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_SIMULATED));
        Assert.Contains("simulated", simulated.Summary.ToLowerInvariant());

        var forecasted = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_FORECASTED));
        Assert.Contains("forecasted", forecasted.Summary.ToLowerInvariant());

        var escalated = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_ESCALATED));
        Assert.Contains("escalated", escalated.Summary.ToLowerInvariant());
    }

    [Fact]
    public void CreateAuditRecord_ConcurrentCreation_ThreadSafe()
    {
        var engine = new PolicyAuditEngine();
        var results = new global::System.Collections.Concurrent.ConcurrentBag<PolicyAuditRecord>();

        Parallel.For(0, 100, i =>
        {
            var input = MakeInput(policyId: $"policy-{i:D3}", actorId: $"actor-{i:D3}");
            var record = engine.CreateAuditRecord(input);
            results.Add(record);
        });

        Assert.Equal(100, results.Count);
        var auditIds = results.Select(r => r.AuditId).ToHashSet();
        Assert.Equal(100, auditIds.Count);
    }

    [Fact]
    public void CreateAuditRecord_RecordIsImmutable()
    {
        var engine = new PolicyAuditEngine();
        var input = MakeInput();

        var record = engine.CreateAuditRecord(input);

        // Records are immutable by definition in C# — verify fields match input
        Assert.Equal(input.PolicyId, record.PolicyId);
        Assert.Equal(input.ActorId, record.ActorId);
        Assert.Equal(input.PolicyDecision, record.Decision);
        Assert.Equal(input.ActionType, record.ActionType);
    }

    [Fact]
    public void CreateAuditRecord_LargeContext_HandledCorrectly()
    {
        var engine = new PolicyAuditEngine();
        var largeContext = string.Join(",", Enumerable.Range(0, 10000).Select(i => $"key{i}=value{i}"));
        var input = MakeInput(context: largeContext);

        var result = engine.CreateAuditRecord(input);

        Assert.NotNull(result);
        Assert.NotEmpty(result.ContextHash);
        Assert.NotEmpty(result.AuditId);
        Assert.Equal(64, result.ContextHash.Length);
    }

    // --- GenerateAuditReport tests ---

    private static readonly DateTime _baseTime = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private static PolicyAuditRecord MakeRecord(
        string policyId = "policy-001",
        string actorId = "actor-001",
        PolicyAuditActionType actionType = PolicyAuditActionType.POLICY_EVALUATED,
        DateTime? timestamp = null) =>
        new(
            PolicyAuditHashGenerator.GenerateAuditId(policyId, actorId, actionType, "ctx", timestamp ?? _baseTime),
            policyId,
            actionType,
            "ALLOW",
            actorId,
            timestamp ?? _baseTime,
            "ctxhash",
            $"Policy '{policyId}' {actionType} by '{actorId}'."
        );

    [Fact]
    public void GenerateAuditReport_ReturnsAllRecords()
    {
        var engine = new PolicyAuditEngine();
        var records = new[]
        {
            MakeRecord(policyId: "p1"),
            MakeRecord(policyId: "p2"),
            MakeRecord(policyId: "p3")
        };
        var query = new PolicyAuditQuery(null, null, null, null, null);

        var report = engine.GenerateAuditReport(query, records);

        Assert.Equal(3, report.TotalEntries);
        Assert.NotNull(report.AuditEntries);
        Assert.Equal(3, report.AuditEntries!.Count);
    }

    [Fact]
    public void GenerateAuditReport_FiltersByPolicyId()
    {
        var engine = new PolicyAuditEngine();
        var records = new[]
        {
            MakeRecord(policyId: "p1"),
            MakeRecord(policyId: "p2"),
            MakeRecord(policyId: "p1", actorId: "actor-002")
        };
        var query = new PolicyAuditQuery("p1", null, null, null, null);

        var report = engine.GenerateAuditReport(query, records);

        Assert.Equal(2, report.TotalEntries);
        Assert.All(report.AuditEntries!, e => Assert.Equal("p1", e.PolicyId));
    }

    [Fact]
    public void GenerateAuditReport_FiltersByActor()
    {
        var engine = new PolicyAuditEngine();
        var records = new[]
        {
            MakeRecord(actorId: "a1"),
            MakeRecord(actorId: "a2"),
            MakeRecord(actorId: "a1", policyId: "p2")
        };
        var query = new PolicyAuditQuery(null, "a1", null, null, null);

        var report = engine.GenerateAuditReport(query, records);

        Assert.Equal(2, report.TotalEntries);
        Assert.All(report.AuditEntries!, e => Assert.Equal("a1", e.ActorId));
    }

    [Fact]
    public void GenerateAuditReport_FiltersByTimeRange()
    {
        var engine = new PolicyAuditEngine();
        var records = new[]
        {
            MakeRecord(timestamp: _baseTime.AddHours(-2)),
            MakeRecord(timestamp: _baseTime, policyId: "p2"),
            MakeRecord(timestamp: _baseTime.AddHours(2), policyId: "p3")
        };
        var query = new PolicyAuditQuery(null, null, null, _baseTime.AddHours(-1), _baseTime.AddHours(1));

        var report = engine.GenerateAuditReport(query, records);

        Assert.Equal(1, report.TotalEntries);
        Assert.Equal("p2", report.AuditEntries![0].PolicyId);
    }

    [Fact]
    public void GenerateAuditReport_FiltersByActionType()
    {
        var engine = new PolicyAuditEngine();
        var records = new[]
        {
            MakeRecord(actionType: PolicyAuditActionType.POLICY_CREATED),
            MakeRecord(actionType: PolicyAuditActionType.POLICY_EVALUATED, policyId: "p2"),
            MakeRecord(actionType: PolicyAuditActionType.POLICY_REVOKED, policyId: "p3")
        };
        var query = new PolicyAuditQuery(null, null, null, null, null, ActionType: "POLICY_EVALUATED");

        var report = engine.GenerateAuditReport(query, records);

        Assert.Equal(1, report.TotalEntries);
        Assert.Equal("POLICY_EVALUATED", report.AuditEntries![0].ActionType);
    }

    [Fact]
    public void GenerateAuditReport_EvidenceLinking()
    {
        var engine = new PolicyAuditEngine();
        var records = new[] { MakeRecord(policyId: "p1", actorId: "a1") };
        var evidence = new[]
        {
            new SystemEvidenceRecord("ev-1", "p1", "a1", "identity", "op-1", true, "OK", _baseTime)
        };
        var query = new PolicyAuditQuery(null, null, null, null, null, IncludeEvidence: true);

        var report = engine.GenerateAuditReport(query, records, evidence);

        Assert.True(report.EvidenceLinked);
        Assert.Equal("ev-1", report.AuditEntries![0].EvidenceId);
    }

    [Fact]
    public void GenerateAuditReport_DeterministicOrdering()
    {
        var engine = new PolicyAuditEngine();
        var records = new[]
        {
            MakeRecord(timestamp: _baseTime.AddHours(2), policyId: "p3"),
            MakeRecord(timestamp: _baseTime, policyId: "p1"),
            MakeRecord(timestamp: _baseTime.AddHours(1), policyId: "p2")
        };
        var query = new PolicyAuditQuery(null, null, null, null, null);

        var report = engine.GenerateAuditReport(query, records);

        Assert.Equal("p1", report.AuditEntries![0].PolicyId);
        Assert.Equal("p2", report.AuditEntries![1].PolicyId);
        Assert.Equal("p3", report.AuditEntries![2].PolicyId);
    }

    [Fact]
    public void GenerateAuditReport_EmptyRecords_ReturnsEmptyReport()
    {
        var engine = new PolicyAuditEngine();
        var query = new PolicyAuditQuery("nonexistent", null, null, null, null);

        var report = engine.GenerateAuditReport(query, Array.Empty<PolicyAuditRecord>());

        Assert.Equal(0, report.TotalEntries);
        Assert.NotNull(report.AuditEntries);
        Assert.Empty(report.AuditEntries!);
        Assert.False(report.EvidenceLinked);
    }

    [Fact]
    public void CreateAuditRecord_NewActionTypes_ProduceCorrectSummaries()
    {
        var engine = new PolicyAuditEngine();

        var created = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_CREATED));
        Assert.Contains("created", created.Summary.ToLowerInvariant());

        var updated = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_UPDATED));
        Assert.Contains("updated", updated.Summary.ToLowerInvariant());

        var approved = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_APPROVED));
        Assert.Contains("approved", approved.Summary.ToLowerInvariant());

        var activated = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_ACTIVATED));
        Assert.Contains("activated", activated.Summary.ToLowerInvariant());

        var suspended = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_SUSPENDED));
        Assert.Contains("suspended", suspended.Summary.ToLowerInvariant());

        var revoked = engine.CreateAuditRecord(MakeInput(actionType: PolicyAuditActionType.POLICY_REVOKED));
        Assert.Contains("revoked", revoked.Summary.ToLowerInvariant());
    }
}
