namespace Whycespace.Engines.T2E.HEOS;

public sealed record AssignmentDecision(
    bool Assigned,
    string Reason,
    Guid WorkerId,
    Guid TaskId
)
{
    public static AssignmentDecision Success(Guid workerId, Guid taskId)
        => new(true, "Worker assigned successfully", workerId, taskId);

    public static AssignmentDecision Rejected(Guid workerId, Guid taskId, string reason)
        => new(false, reason, workerId, taskId);
}
