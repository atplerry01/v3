namespace Whycespace.Domain.Governance;

public sealed record Policy(
    Guid PolicyId,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<GovernanceRule> Rules,
    PolicyStatus Status,
    DateTimeOffset CreatedAt
);

public enum PolicyStatus
{
    Active,
    Draft,
    Archived
}
