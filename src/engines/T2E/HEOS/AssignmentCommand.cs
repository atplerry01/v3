namespace Whycespace.Engines.T2E.HEOS;

public sealed record AssignmentCommand(
    Guid WorkforceId,
    Guid TaskId,
    string TaskType,
    Guid RequestedByOperatorId,
    string AssignmentScope
);
