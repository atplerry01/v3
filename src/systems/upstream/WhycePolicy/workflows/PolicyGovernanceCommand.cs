namespace Whycespace.Systems.Upstream.WhycePolicy.Workflows;

using Whycespace.Contracts.Commands;

public sealed record PolicyGovernanceCommand(
    Guid CommandId,
    DateTimeOffset Timestamp,
    string PolicyDefinition,
    string SubmittedBy,
    string SubmissionReason,
    string GovernanceDomain
) : ICommand;
