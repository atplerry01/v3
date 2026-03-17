namespace Whycespace.Engines.T2E.Workforce.Models;

public sealed record WorkforceLifecycleDecision(
    bool Success,
    string Reason,
    string PreviousStatus,
    string NewStatus,
    DateTimeOffset Timestamp
)
{
    public static WorkforceLifecycleDecision Accepted(
        string previousStatus, string newStatus, DateTimeOffset timestamp)
        => new(true, "Lifecycle transition accepted", previousStatus, newStatus, timestamp);

    public static WorkforceLifecycleDecision Rejected(
        string previousStatus, string reason, DateTimeOffset timestamp)
        => new(false, reason, previousStatus, previousStatus, timestamp);
}
