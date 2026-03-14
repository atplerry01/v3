using Whycespace.Reliability.DeadLetter.Models;
using Whycespace.Reliability.Recovery.Models;

namespace Whycespace.Reliability.Recovery.Engine;

public sealed class EventRecoveryEngine
{
    private const int MaxReplayCount = 2;

    public RecoveryDecision Evaluate(DeadLetterEvent deadLetterEvent, int replayCount)
    {
        if (replayCount > MaxReplayCount)
        {
            return new RecoveryDecision(
                AllowReplay: false,
                Quarantine: true,
                Reason: $"Replay count {replayCount} exceeds maximum of {MaxReplayCount}"
            );
        }

        if (deadLetterEvent.Reason == DeadLetterReason.SchemaViolation)
        {
            return new RecoveryDecision(
                AllowReplay: false,
                Quarantine: true,
                Reason: "Schema violation cannot be resolved by replay"
            );
        }

        if (deadLetterEvent.Reason == DeadLetterReason.InvalidPayload)
        {
            return new RecoveryDecision(
                AllowReplay: false,
                Quarantine: true,
                Reason: "Invalid payload cannot be resolved by replay"
            );
        }

        if (deadLetterEvent.Reason == DeadLetterReason.PolicyViolation)
        {
            return new RecoveryDecision(
                AllowReplay: false,
                Quarantine: true,
                Reason: "Policy violation requires manual review"
            );
        }

        return new RecoveryDecision(
            AllowReplay: true,
            Quarantine: false,
            Reason: "Event eligible for replay"
        );
    }
}
