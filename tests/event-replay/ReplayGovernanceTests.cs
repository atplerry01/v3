using Whycespace.EventReplay.Governance.Engine;
using Whycespace.EventReplay.Governance.Models;

namespace Whycespace.EventReplay.Tests;

public class ReplayGovernanceTests
{
    private readonly EventReplayGovernanceEngine _engine = new();

    [Fact]
    public void EvaluateReplay_ValidRequest_AllowsReplay()
    {
        var request = new ReplayRequest(Guid.NewGuid(), "whyce.engine.events", "{\"amount\":100}", 0);

        var decision = _engine.EvaluateReplay(request);

        Assert.True(decision.AllowReplay);
        Assert.False(decision.Quarantine);
    }

    [Fact]
    public void EvaluateReplay_ReplayCountAtLimit_AllowsReplay()
    {
        var request = new ReplayRequest(Guid.NewGuid(), "whyce.engine.events", "{\"amount\":100}", 2);

        var decision = _engine.EvaluateReplay(request);

        Assert.True(decision.AllowReplay);
        Assert.False(decision.Quarantine);
    }

    [Fact]
    public void EvaluateReplay_ReplayCountExceedsLimit_Quarantines()
    {
        var request = new ReplayRequest(Guid.NewGuid(), "whyce.engine.events", "{\"amount\":100}", 3);

        var decision = _engine.EvaluateReplay(request);

        Assert.False(decision.AllowReplay);
        Assert.True(decision.Quarantine);
        Assert.Contains("exceeds maximum", decision.Reason);
    }

    [Fact]
    public void EvaluateReplay_HighReplayCount_Quarantines()
    {
        var request = new ReplayRequest(Guid.NewGuid(), "whyce.engine.events", "{\"data\":1}", 10);

        var decision = _engine.EvaluateReplay(request);

        Assert.False(decision.AllowReplay);
        Assert.True(decision.Quarantine);
    }

    [Fact]
    public void EvaluateReplay_EmptyPayload_Quarantines()
    {
        var request = new ReplayRequest(Guid.NewGuid(), "whyce.engine.events", "", 0);

        var decision = _engine.EvaluateReplay(request);

        Assert.False(decision.AllowReplay);
        Assert.True(decision.Quarantine);
        Assert.Contains("Payload", decision.Reason);
    }

    [Fact]
    public void EvaluateReplay_WhitespacePayload_Quarantines()
    {
        var request = new ReplayRequest(Guid.NewGuid(), "whyce.engine.events", "   ", 0);

        var decision = _engine.EvaluateReplay(request);

        Assert.False(decision.AllowReplay);
        Assert.True(decision.Quarantine);
    }

    [Fact]
    public void EvaluateReplay_EmptyEventId_Rejects()
    {
        var request = new ReplayRequest(Guid.Empty, "whyce.engine.events", "{\"amount\":100}", 0);

        var decision = _engine.EvaluateReplay(request);

        Assert.False(decision.AllowReplay);
        Assert.False(decision.Quarantine);
        Assert.Contains("EventId", decision.Reason);
    }

    [Fact]
    public void EvaluateReplay_EmptySourceTopic_Rejects()
    {
        var request = new ReplayRequest(Guid.NewGuid(), "", "{\"amount\":100}", 0);

        var decision = _engine.EvaluateReplay(request);

        Assert.False(decision.AllowReplay);
        Assert.False(decision.Quarantine);
        Assert.Contains("SourceTopic", decision.Reason);
    }

    [Fact]
    public void EvaluateReplay_PreservesEventIdentity()
    {
        var eventId = Guid.NewGuid();
        var request = new ReplayRequest(eventId, "whyce.engine.events", "{\"amount\":100}", 1);

        var decision = _engine.EvaluateReplay(request);

        Assert.True(decision.AllowReplay);
        Assert.Equal(eventId, request.EventId);
    }

    [Fact]
    public void EvaluateReplay_IsDeterministic()
    {
        var eventId = Guid.NewGuid();
        var request = new ReplayRequest(eventId, "whyce.engine.events", "{\"amount\":100}", 1);

        var decision1 = _engine.EvaluateReplay(request);
        var decision2 = _engine.EvaluateReplay(request);

        Assert.Equal(decision1, decision2);
    }

    [Fact]
    public void EvaluateReplay_DifferentInstances_SameResult()
    {
        var request = new ReplayRequest(Guid.NewGuid(), "whyce.engine.events", "{\"amount\":100}", 1);

        var engine1 = new EventReplayGovernanceEngine();
        var engine2 = new EventReplayGovernanceEngine();

        var decision1 = engine1.EvaluateReplay(request);
        var decision2 = engine2.EvaluateReplay(request);

        Assert.Equal(decision1, decision2);
    }

    [Fact]
    public void ReplayMetadata_TracksReplayHistory()
    {
        var eventId = Guid.NewGuid();
        var first = DateTime.UtcNow.AddMinutes(-10);
        var last = DateTime.UtcNow;

        var metadata = new ReplayMetadata(eventId, 2, first, last);

        Assert.Equal(eventId, metadata.EventId);
        Assert.Equal(2, metadata.ReplayCount);
        Assert.Equal(first, metadata.FirstReplay);
        Assert.Equal(last, metadata.LastReplay);
    }

    [Fact]
    public void ReplayDecision_RecordEquality()
    {
        var d1 = new ReplayDecision(true, false, "Approved");
        var d2 = new ReplayDecision(true, false, "Approved");

        Assert.Equal(d1, d2);
    }
}
