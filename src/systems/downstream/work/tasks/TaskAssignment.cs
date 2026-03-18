namespace Whycespace.Systems.Downstream.Work.Tasks;

public sealed record TaskAssignment(
    Guid AssignmentId,
    Guid TaskId,
    string WorkerId,
    string Status,
    DateTimeOffset AssignedAt,
    DateTimeOffset? CompletedAt = null
);
