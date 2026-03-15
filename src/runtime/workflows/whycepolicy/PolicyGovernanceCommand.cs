namespace Whycespace.Runtime.Workflows.WhycePolicy;

using Whycespace.Contracts.Commands;

public sealed record PolicyGovernanceCommand(
    Guid CommandId,
    DateTimeOffset Timestamp,
    string PolicyDefinition,
    string SubmittedBy,
    string SubmissionReason,
    string GovernanceDomain
) : ICommand;
