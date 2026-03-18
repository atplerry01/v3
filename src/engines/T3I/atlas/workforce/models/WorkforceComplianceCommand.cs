namespace Whycespace.Engines.T3I.Atlas.Workforce.Models;

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
