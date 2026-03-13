namespace Whycespace.Engines.T3I.HEOS;

public sealed record WorkforceComplianceCommand(
    Guid WorkforceId,
    string WorkerStatus,
    IReadOnlyList<string> Capabilities,
    int CompletedTasks,
    int FailedTasks,
    DateTimeOffset CompliancePeriodStart,
    DateTimeOffset CompliancePeriodEnd,
    DateTimeOffset? LastPolicyReviewDate
);
