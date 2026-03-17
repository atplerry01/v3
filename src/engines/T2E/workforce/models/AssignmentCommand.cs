namespace Whycespace.Engines.T2E.Workforce.Models;

public sealed record AssignmentCommand(
    Guid WorkforceId,
    Guid TaskId,
    string TaskType,
    Guid RequestedByOperatorId,
    string AssignmentScope
);
