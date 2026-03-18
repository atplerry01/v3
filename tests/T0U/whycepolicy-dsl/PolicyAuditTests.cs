using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyAuditTests
{
    private readonly PolicyEvidenceStore _evidenceStore = new();
    private readonly PolicyEvidenceRecorderEngine _recorder;
    private readonly PolicyAuditEngine _auditEngine;

    public PolicyAuditTests()
    {
        _recorder = new PolicyEvidenceRecorderEngine(_evidenceStore);
        _auditEngine = new PolicyAuditEngine(_evidenceStore);
    }

    [Fact]
    public void AuditPolicy_ByPolicyId_ReturnsMatching()
    {
        _recorder.RecordPolicyEvidence("pol-1", "actor-1", "identity", "op-1", true, "OK");
        _recorder.RecordPolicyEvidence("pol-2", "actor-1", "identity", "op-2", true, "OK");

        var report = _auditEngine.AuditPolicy(new PolicyAuditQuery("pol-1", null, null, null, null));

        Assert.Equal(1, report.TotalRecords);
        Assert.All(report.EvidenceRecords, r => Assert.Equal("pol-1", r.PolicyId));
    }

    [Fact]
    public void AuditPolicy_ByActorId_ReturnsMatching()
    {
        _recorder.RecordPolicyEvidence("pol-a", "actor-A", "identity", "op-1", true, "OK");
        _recorder.RecordPolicyEvidence("pol-b", "actor-B", "identity", "op-2", false, "Denied");

        var report = _auditEngine.AuditPolicy(new PolicyAuditQuery(null, "actor-A", null, null, null));

        Assert.Equal(1, report.TotalRecords);
        Assert.All(report.EvidenceRecords, r => Assert.Equal("actor-A", r.ActorId));
    }

    [Fact]
    public void AuditPolicy_ByDomain_ReturnsMatching()
    {
        _recorder.RecordPolicyEvidence("pol-x", "actor-1", "identity", "op-1", true, "OK");
        _recorder.RecordPolicyEvidence("pol-y", "actor-2", "clusters", "op-2", true, "OK");
        _recorder.RecordPolicyEvidence("pol-z", "actor-3", "identity", "op-3", false, "No");

        var report = _auditEngine.AuditPolicy(new PolicyAuditQuery(null, null, "identity", null, null));

        Assert.Equal(2, report.TotalRecords);
        Assert.All(report.EvidenceRecords, r => Assert.Equal("identity", r.Domain));
    }

    [Fact]
    public void AuditPolicy_ByDateRange_ReturnsMatching()
    {
        _recorder.RecordPolicyEvidence("pol-date", "actor-1", "identity", "op-1", true, "OK");

        var now = DateTime.UtcNow;
        var report = _auditEngine.AuditPolicy(new PolicyAuditQuery(
            null, null, null, now.AddMinutes(-1), now.AddMinutes(1)));

        Assert.True(report.TotalRecords >= 1);
    }

    [Fact]
    public void AuditPolicy_MultipleFilters_CombinesCorrectly()
    {
        _recorder.RecordPolicyEvidence("pol-combo", "actor-combo", "identity", "op-1", true, "OK");
        _recorder.RecordPolicyEvidence("pol-combo", "actor-other", "identity", "op-2", true, "OK");
        _recorder.RecordPolicyEvidence("pol-other", "actor-combo", "identity", "op-3", true, "OK");

        var report = _auditEngine.AuditPolicy(new PolicyAuditQuery("pol-combo", "actor-combo", null, null, null));

        Assert.Equal(1, report.TotalRecords);
        Assert.Equal("pol-combo", report.EvidenceRecords[0].PolicyId);
        Assert.Equal("actor-combo", report.EvidenceRecords[0].ActorId);
    }

    [Fact]
    public void AuditPolicy_ReturnsCorrectRecordCount()
    {
        _recorder.RecordPolicyEvidence("pol-count", "actor-1", "identity", "op-1", true, "OK");
        _recorder.RecordPolicyEvidence("pol-count", "actor-2", "identity", "op-2", false, "No");
        _recorder.RecordPolicyEvidence("pol-count", "actor-3", "identity", "op-3", true, "OK");

        var report = _auditEngine.AuditPolicy(new PolicyAuditQuery("pol-count", null, null, null, null));

        Assert.Equal(3, report.TotalRecords);
        Assert.Equal(report.EvidenceRecords.Count, report.TotalRecords);
    }

    [Fact]
    public void AuditPolicy_NoMatches_ReturnsEmptyResult()
    {
        _recorder.RecordPolicyEvidence("pol-exists", "actor-1", "identity", "op-1", true, "OK");

        var report = _auditEngine.AuditPolicy(new PolicyAuditQuery("nonexistent", null, null, null, null));

        Assert.Equal(0, report.TotalRecords);
        Assert.Empty(report.EvidenceRecords);
    }
}
