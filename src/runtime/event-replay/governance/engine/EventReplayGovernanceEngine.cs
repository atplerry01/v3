using Whycespace.EventReplay.Governance.Models;

namespace Whycespace.EventReplay.Governance.Engine;

public sealed class EventReplayGovernanceEngine
{
    private const int MaxReplayCount = 2;

    public ReplayDecision EvaluateReplay(ReplayRequest request)
    {
        if (request.ReplayCount > MaxReplayCount)
        {
            return new ReplayDecision(
                AllowReplay: false,
                Quarantine: true,
                Reason: $"Replay count {request.ReplayCount} exceeds maximum of {MaxReplayCount}. Event quarantined.");
        }

        if (string.IsNullOrWhiteSpace(request.Payload))
        {
            return new ReplayDecision(
                AllowReplay: false,
                Quarantine: true,
                Reason: "Payload is empty or invalid. Event quarantined.");
        }

        if (request.EventId == Guid.Empty)
        {
            return new ReplayDecision(
                AllowReplay: false,
                Quarantine: false,
                Reason: "EventId is empty. Replay rejected.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceTopic))
        {
            return new ReplayDecision(
                AllowReplay: false,
                Quarantine: false,
                Reason: "SourceTopic is empty. Replay rejected.");
        }

        return new ReplayDecision(
            AllowReplay: true,
            Quarantine: false,
            Reason: "Replay approved. Event meets governance requirements.");
    }
}
