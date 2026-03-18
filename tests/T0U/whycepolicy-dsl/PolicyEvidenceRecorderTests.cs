using Whycespace.Engines.T0U.WhycePolicy.Monitoring.Evidence;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyEvidenceRecorderTests
{
    private readonly PolicyEvidenceStore _store = new();
    private readonly PolicyEvidenceRecorderEngine _engine;

    public PolicyEvidenceRecorderTests()
    {
        _engine = new PolicyEvidenceRecorderEngine(_store);
    }

    [Fact]
    public void RecordPolicyEvidence_CreatesRecord()
    {
        var record = _engine.RecordPolicyEvidence(
            "pol-1", "actor-1", "identity", "update-profile", false, "Trust too low");

        Assert.NotNull(record);
        Assert.StartsWith("ev-", record.EvidenceId);
        Assert.Equal("pol-1", record.PolicyId);
        Assert.Equal("actor-1", record.ActorId);
        Assert.Equal("identity", record.Domain);
        Assert.Equal("update-profile", record.Operation);
        Assert.False(record.Allowed);
        Assert.Equal("Trust too low", record.Reason);
    }

    [Fact]
    public void GetEvidence_ReturnsStoredRecord()
    {
        var created = _engine.RecordPolicyEvidence(
            "pol-2", "actor-2", "clusters", "create-cluster", true, "Allowed");

        var retrieved = _engine.GetEvidence(created.EvidenceId);

        Assert.NotNull(retrieved);
        Assert.Equal(created.EvidenceId, retrieved.EvidenceId);
        Assert.Equal(created.PolicyId, retrieved.PolicyId);
    }

    [Fact]
    public void GetAllEvidence_ReturnsMultipleRecords()
    {
        _engine.RecordPolicyEvidence("pol-a", "actor-1", "identity", "op-1", true, "OK");
        _engine.RecordPolicyEvidence("pol-b", "actor-2", "clusters", "op-2", false, "Denied");
        _engine.RecordPolicyEvidence("pol-c", "actor-3", "economic", "op-3", true, "OK");

        var all = _engine.GetAllEvidence();

        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void RecordPolicyEvidence_EvidenceIdIsUnique()
    {
        var record1 = _engine.RecordPolicyEvidence(
            "pol-uniq", "actor-1", "identity", "op-1", true, "OK");
        var record2 = _engine.RecordPolicyEvidence(
            "pol-uniq", "actor-1", "identity", "op-1", true, "OK");

        Assert.NotEqual(record1.EvidenceId, record2.EvidenceId);
    }

    [Fact]
    public void RecordPolicyEvidence_FieldsCorrectlyRecorded()
    {
        var record = _engine.RecordPolicyEvidence(
            "pol-fields", "actor-fields", "economic", "transfer-funds", false, "Insufficient clearance");

        Assert.Equal("pol-fields", record.PolicyId);
        Assert.Equal("actor-fields", record.ActorId);
        Assert.Equal("economic", record.Domain);
        Assert.Equal("transfer-funds", record.Operation);
        Assert.False(record.Allowed);
        Assert.Equal("Insufficient clearance", record.Reason);
        Assert.True(record.RecordedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void RecordPolicyEvidence_ConcurrentWrites_ThreadSafe()
    {
        var tasks = new List<Task>();
        for (var i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() => _engine.RecordPolicyEvidence(
                $"pol-{index}", $"actor-{index}", "identity", "op", true, "OK")));
        }

        Task.WaitAll(tasks.ToArray());

        var all = _engine.GetAllEvidence();
        Assert.Equal(100, all.Count);
    }
}
